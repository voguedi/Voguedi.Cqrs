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

namespace Voguedi.Domain.Events
{
    class ProcessingEventHandler : IProcessingEventHandler
    {
        #region Private Fields

        readonly IEventVersionStore versionStore;
        readonly IServiceProvider serviceProvider;
        readonly ILogger logger;
        readonly ConcurrentDictionary<Type, IReadOnlyList<IEventHandler>> handlerMapping;

        #endregion

        #region Ctors

        public ProcessingEventHandler(IEventVersionStore versionStore, IServiceProvider serviceProvider, ILogger<ProcessingEventHandler> logger)
        {
            this.versionStore = versionStore;
            this.serviceProvider = serviceProvider;
            this.logger = logger;
            handlerMapping = new ConcurrentDictionary<Type, IReadOnlyList<IEventHandler>>();
        }

        #endregion

        #region Private Methods

        Task DispatchEventAsync(ProcessingEvent processingEvent)
        {
            var events = processingEvent.Stream.Events;
            
            if (events?.Count > 0)
            {
                using (var serviceScope = serviceProvider.CreateScope())
                {
                    var queue = BuildHandlingEventQueue(events, serviceScope);

                    if (!queue.IsCompleted && queue.TryTake(out var current))
                        return HandleEventAsync(processingEvent, current, queue);

                    return SaveVersionAsync(processingEvent);
                }
            }

            return processingEvent.OnQueueProcessedAsync();
        }

        BlockingCollection<(IEvent Event, IEventHandler Handler)> BuildHandlingEventQueue(IReadOnlyList<IEvent> events, IServiceScope serviceScope)
        {
            var queue = new BlockingCollection<(IEvent Event, IEventHandler Handler)>(new ConcurrentQueue<(IEvent Event, IEventHandler Handler)>());

            foreach (var e in events)
            {
                foreach (var handler in GetHandlers(e, serviceScope))
                    queue.TryAdd((e, handler));
            }

            return queue;
        }

        IReadOnlyList<IEventHandler> GetHandlers(IEvent e, IServiceScope serviceScope)
        {
            var handlers = new List<IEventHandler>();
            var values = handlerMapping.GetOrAddIfNotNull(
                e.GetType(),
                key =>
                {
                    var handlerType = typeof(IEventHandler<>).GetTypeInfo().MakeGenericType(key);
                    return serviceScope.ServiceProvider.GetServices(handlerType)?.Cast<IEventHandler>()?.ToList();
                });

            if (values?.Count > 0)
                handlers.AddRange(values);

            return handlers;
        }

        async Task HandleEventAsync(ProcessingEvent processingEvent, (IEvent Event, IEventHandler Handler) current, BlockingCollection<(IEvent Event, IEventHandler Handler)> queue)
        {
            var e = current.Event;
            var eventType = e.GetType();
            var handler = current.Handler;
            var handlerType = handler.GetType();
            var handlerMethod = handlerType.GetTypeInfo().GetMethod("HandleAsync", new[] { eventType });
            var result = await Policy
                .Handle<Exception>()
                .OrResult<AsyncExecutedResult>(r => !r.Succeeded)
                .WaitAndRetryForeverAsync(retryAttempt => TimeSpan.FromSeconds(Math.Pow(1, retryAttempt)),
                    (delegateResult, retryCount, retryAttempt) => logger.LogError(delegateResult.Exception ?? delegateResult.Result.Exception, $"事件处理器执行失败，重试。 [EventType = {eventType}, EventId = {e.Id}, EventHandlerType = {handlerType}, RetryCount = {retryCount}, RetryAttempt = {retryAttempt}]"))
                .ExecuteAsync(() => (Task<AsyncExecutedResult>)handlerMethod.Invoke(handler, new object[] { e }));

            if (result.Succeeded)
            {
                logger.LogDebug($"事件处理器执行成功。 [EventType = {eventType}, EventId = {e.Id}, EventHandlerType = {handlerType}]");

                if (!queue.IsCompleted && queue.TryTake(out var next) && next.Event != null && next.Handler != null)
                    await HandleEventAsync(processingEvent, next, queue);
                else
                    await SaveVersionAsync(processingEvent);
            }
            else
                logger.LogError(result.Exception, $"事件处理器执行失败。 [EventType = {eventType}, EventId = {e.Id}, EventHandlerType = {handlerType}]");
        }

        async Task SaveVersionAsync(ProcessingEvent processingEvent)
        {
            var stream = processingEvent.Stream;
            var result = await Policy
                .Handle<Exception>()
                .OrResult<AsyncExecutedResult>(r => !r.Succeeded)
                .WaitAndRetryForeverAsync(retryAttempt => TimeSpan.FromSeconds(Math.Pow(1, retryAttempt)),
                    (delegateResult, retryCount, retryAttempt) => logger.LogError(delegateResult.Exception ?? delegateResult.Result.Exception, $"事件版本存储失败，重试。 [EventStream = {stream}, RetryCount = {retryCount}, RetryAttempt = {retryAttempt}]"))
                .ExecuteAsync(() => versionStore.SaveAsync(stream.AggregateRootTypeName, stream.AggregateRootId, stream.Version));

            if (result.Succeeded)
            {
                logger.LogDebug($"事件版本存储成功。 {stream}");
                await processingEvent.OnQueueProcessedAsync();
            }
            else
                logger.LogError(result.Exception, $"事件版本存储失败。 {stream}");
        }

        #endregion

        #region IProcessingEventHandler

        public async Task HandleAsync(ProcessingEvent processingEvent)
        {
            var stream = processingEvent.Stream;
            var streamVersion = stream.Version;
            var result = await Policy
                .Handle<Exception>()
                .OrResult<AsyncExecutedResult<long>>(r => !r.Succeeded)
                .WaitAndRetryForeverAsync(retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (delegateResult, retryCount, retryAttempt) => logger.LogError(delegateResult.Exception ?? delegateResult.Result.Exception, $"获取已发布事件版本失败。 [EventStream = {stream}, RetryCount = {retryCount}, RetryAttempt = {retryAttempt}]"))
                .ExecuteAsync(() => versionStore.GetAsync(stream.AggregateRootTypeName, stream.AggregateRootId));

            if (result.Succeeded)
            {
                var storedVersion = result.Data;
                var exceptedVersion = storedVersion + 1;

                if (streamVersion == exceptedVersion)
                {
                    logger.LogDebug($"获取事件版本成功。 {stream}");
                    await DispatchEventAsync(processingEvent);
                }
                else if (streamVersion > exceptedVersion)
                {
                    logger.LogDebug($"当前事件版本大于待处理版本，等待处理。 [StoredVersion = {storedVersion}, ProcessingEventStream = {stream}]");
                    processingEvent.EnqueueToWaitingQueue();
                }
                else
                {
                    logger.LogError($"当前事件版本小于待处理版本，处理失败。 [StoredVersion = {storedVersion}, ProcessingEventStream = {stream}]");
                    await processingEvent.OnQueueProcessedAsync();
                }
            }
            else
                logger.LogError(result.Exception, $"获取事件版本失败。 {stream}");
        }

        #endregion
    }
}
