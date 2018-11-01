using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Voguedi.Domain.Events
{
    class ProcessingDomainEventHandler : IProcessingDomainEventHandler
    {
        #region Private Fields

        readonly IDomainEventPublishedVersionStore versionStore;
        readonly IServiceProvider serviceProvider;
        readonly ILogger logger;
        readonly ConcurrentDictionary<Type, IEnumerable<IDomainEventHandler>> handlerMapping = new ConcurrentDictionary<Type, IEnumerable<IDomainEventHandler>>();

        #endregion

        #region Private Methods

        Task ProcessEventAsync(ProcessingDomainEvent processingEvent)
        {
            return null;
        }

        #endregion

        #region IProcessingDomainEventHandler

        public async Task HandleAsync(ProcessingDomainEvent processingEvent)
        {
            var stream = processingEvent.Stream;
            var streamVersion = stream.Version;
            var result = await versionStore.GetAsync(stream.AggregateRootTypeName, stream.AggregateRootId);

            if (result.Succeeded)
            {
                var currentVersion = result.Data;
                var exceptedVersion = currentVersion + 1;

                if (streamVersion == exceptedVersion)
                {
                    logger.LogInformation($"获取已发布领域事件版本成功！ {stream}");
                    await ProcessEventAsync(processingEvent);
                }
                else if (streamVersion > exceptedVersion)
                {
                    logger.LogInformation($"当前领域事件版本大于待处理版本！ [CurrentPublishedVersion = {currentVersion}, DomainEventStream = {stream}]");
                    processingEvent.EnqueueToWaitingQueue();
                }
                else
                {
                    logger.LogError($"当前领域事件版本小于待处理版本！ [CurrentPublishedVersion = {currentVersion}, DomainEventStream = {stream}]");
                    await processingEvent.OnQueueRejectedAsync();
                }
            }
            else
            {
                logger.LogError(result.Exception, $"获取已发布领域事件版本失败！ {stream}");
                await processingEvent.OnQueueRejectedAsync();
            }
        }

        #endregion
    }
}
