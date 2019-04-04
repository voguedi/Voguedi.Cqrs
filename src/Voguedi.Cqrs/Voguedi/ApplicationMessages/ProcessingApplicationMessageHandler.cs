using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Voguedi.Infrastructure;

namespace Voguedi.ApplicationMessages
{
    class ProcessingApplicationMessageHandler : IProcessingApplicationMessageHandler
    {
        #region Private Fields
        
        readonly IServiceProvider serviceProvider;
        readonly ILogger logger;
        readonly ConcurrentDictionary<Type, IEnumerable<IApplicationMessageHandler>> handlerMapping;

        #endregion

        #region Ctors

        public ProcessingApplicationMessageHandler(IServiceProvider serviceProvider, ILogger<ProcessingApplicationMessageHandler> logger)
        {
            this.serviceProvider = serviceProvider;
            this.logger = logger;
            handlerMapping = new ConcurrentDictionary<Type, IEnumerable<IApplicationMessageHandler>>();
        }

        #endregion

        #region Private Methods

        IEnumerable<IApplicationMessageHandler> GetHandlers(Type messageType, IServiceScope serviceScope)
        {
            return handlerMapping.GetOrAddIfNotNull(
                messageType,
                key =>
                {
                    var handlerType = typeof(IApplicationMessageHandler<>).GetTypeInfo().MakeGenericType(key);
                    return serviceScope.ServiceProvider.GetServices(handlerType)?.Cast<IApplicationMessageHandler>();
                });
        }

        async Task HandleApplicationMessageAsync(ProcessingApplicationMessage processingMessage, IApplicationMessageHandler current, BlockingCollection<IApplicationMessageHandler> queue)
        {
            var applicationMessage = processingMessage.ApplicationMessage;
            var applicationMessageType = applicationMessage.GetType();
            var handlerType = current.GetType();
            var handlerMethod = handlerType.GetTypeInfo().GetMethod("HandleAsync", new[] { applicationMessageType });
            var result = await Policy
                .Handle<Exception>()
                .OrResult<AsyncExecutedResult>(r => !r.Succeeded)
                .WaitAndRetryForeverAsync(retryAttempt => TimeSpan.FromSeconds(Math.Pow(1, retryAttempt)),
                    (delegateResult, retryCount, retryAttempt) => logger.LogError(delegateResult.Exception ?? delegateResult.Result.Exception, $"应用消息处理器执行失败。 [ApplicationMessageType = {applicationMessageType}, ApplicationMessageId = {applicationMessage.Id}, ApplicationMessageHandlerType = {handlerType}, RetryCount = {retryCount}, RetryAttempt = {retryAttempt}]"))
                .ExecuteAsync(() => (Task<AsyncExecutedResult>)handlerMethod.Invoke(current, new object[] { applicationMessage }));

            if (result.Succeeded)
            {
                logger.LogDebug($"应用消息处理器执行成功。 [ApplicationMessageType = {applicationMessageType}, ApplicationMessageId = {applicationMessage.Id}, ApplicationMessageHandlerType = {handlerType}]");

                if (!queue.IsCompleted && queue.TryTake(out var next) && next != null)
                    await HandleApplicationMessageAsync(processingMessage, next, queue);
                else
                    await processingMessage.OnQueueProcessedAsync();
            }
            else
                logger.LogError(result.Exception, $"应用消息处理器执行失败。 [ApplicationMessageType = {applicationMessageType}, ApplicationMessageId = {applicationMessage.Id}, ApplicationMessageHandlerType = {handlerType}]");
        }

        #endregion

        #region IProcessingApplicationMessageHandler

        public Task HandleAsync(ProcessingApplicationMessage processingApplicationMessage)
        {
            using (var serviceScope = serviceProvider.CreateScope())
            {
                var applicationMessage = processingApplicationMessage.ApplicationMessage;
                var applicationMessageType = applicationMessage.GetType();
                var handlers = GetHandlers(applicationMessageType, serviceScope);

                if (handlers?.Count() > 0)
                {
                    var queue = new BlockingCollection<IApplicationMessageHandler>(new ConcurrentQueue<IApplicationMessageHandler>());

                    foreach (var handler in handlers)
                        queue.TryAdd(handler);

                    if (!queue.IsCompleted && queue.TryTake(out var current))
                        return HandleApplicationMessageAsync(processingApplicationMessage, current, queue);

                    return processingApplicationMessage.OnQueueProcessedAsync();
                }

                logger.LogDebug($"未注册任何应用消息处理器。 [ApplicationMessageType = {applicationMessageType}, ApplicationMessageId = {applicationMessage.Id}]");
                return processingApplicationMessage.OnQueueProcessedAsync();
            }
        }

        #endregion
    }
}
