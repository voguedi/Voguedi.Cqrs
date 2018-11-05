using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Voguedi.AsyncExecution;

namespace Voguedi.Events
{
    class ProcessingEventHandler : IProcessingEventHandler
    {
        #region Private Fields

        readonly IEventVersionStore versionStore;
        readonly IServiceProvider serviceProvider;
        readonly ILogger logger;
        readonly ConcurrentDictionary<Type, IReadOnlyList<IEventHandler>> handlerMapping = new ConcurrentDictionary<Type, IReadOnlyList<IEventHandler>>();
        const int retryTimes = 3;
        const int retryInterval = 1000;

        #endregion

        #region Ctors

        public ProcessingEventHandler(IEventVersionStore versionStore, IServiceProvider serviceProvider, ILogger<ProcessingEventHandler> logger)
        {
            this.versionStore = versionStore;
            this.serviceProvider = serviceProvider;
            this.logger = logger;
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

                    if (queue.TryDequeue(out var current) && current.Event != null && current.Handler != null)
                        return HandleEventAsync(processingEvent, current, queue);
                }
            }

            return processingEvent.OnQueueCommittedAsync();
        }

        ConcurrentQueue<(IEvent Event, IEventHandler Handler)> BuildHandlingEventQueue(IReadOnlyList<IEvent> events, IServiceScope serviceScope)
        {
            var queue = new ConcurrentQueue<(IEvent Event, IEventHandler Handler)>();

            foreach (var e in events)
            {
                foreach (var handler in GetHandlers(e, serviceScope))
                    queue.Enqueue((e, handler));
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

        Task HandleEventAsync(ProcessingEvent processingEvent, (IEvent Event, IEventHandler Handler) current, ConcurrentQueue<(IEvent Event, IEventHandler Handler)> queue)
            => HandleEventAsync(processingEvent, current, queue, 0, 0);

        Task HandleEventAsync(
            ProcessingEvent processingEvent,
            (IEvent Event, IEventHandler Handler) current,
            ConcurrentQueue<(IEvent Event, IEventHandler Handler)> queue,
            int currentRetryInterval,
            int currentRetryTimes)
            => HandleEventAsync(processingEvent, current, queue, async (i, t) => await HandleEventAsync(processingEvent, current, queue, i, t), currentRetryInterval, currentRetryTimes);

        async Task HandleEventAsync(
            ProcessingEvent processingEvent,
            (IEvent Event, IEventHandler Handler) current,
            ConcurrentQueue<(IEvent Event, IEventHandler Handler)> queue,
            Action<int, int> retryAction,
            int currentRetryInterval,
            int currentRetryTimes)
        {
            var e = current.Event;
            var eventType = e.GetType();
            var handler = current.Handler;
            var handlerType = handler.GetType();

            try
            {
                var handlerMethod = handlerType.GetTypeInfo().GetMethod("HandleAsync", new[] { eventType });
                var result = await (Task<AsyncExecutedResult>)handlerMethod.Invoke(handler, new object[] { e });

                if (result.Succeeded)
                {
                    if (queue.Count > 0)
                    {
                        if (queue.TryDequeue(out var next) && next.Event != null && next.Handler != null)
                            await HandleEventAsync(processingEvent, next, queue);
                        else
                            await processingEvent.OnQueueCommittedAsync();
                    }
                    else
                        await processingEvent.OnQueueCommittedAsync();
                }
                else
                    throw result.Exception;
            }
            catch (Exception ex)
            {
                await HandleEventAsync(processingEvent, current, queue, retryAction, currentRetryInterval, currentRetryTimes, ex);
            }
        }

        Task HandleEventAsync(
            ProcessingEvent processingEvent,
            (IEvent Event, IEventHandler Handler) current,
            ConcurrentQueue<(IEvent Event, IEventHandler Handler)> queue,
            Action<int, int> retryAction,
            int currentRetryInterval,
            int currentRetryTimes,
            Exception exception)
        {
            if (currentRetryTimes < retryTimes)
            {
                currentRetryTimes++;
                retryAction?.Invoke(currentRetryInterval, currentRetryTimes);
                return Task.CompletedTask;
            }

            currentRetryInterval += retryInterval;
            currentRetryTimes++;
            var e = current.Event;
            logger.LogError(exception, $"事件处理器执行失败！ [EventType = {e.GetType()}, EventId = {e.Id}, EventHandlerType = {current.Handler.GetType()}, CurrentRetryTimes = {currentRetryTimes}]");
            
            if (retryAction != null)
                return Task.Factory.StartDelayed(currentRetryInterval, () => retryAction(currentRetryInterval, currentRetryTimes));

            return Task.CompletedTask;
        }

        #endregion

        #region IProcessingEventHandler

        public async Task HandleAsync(ProcessingEvent processingEvent)
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
                    logger.LogInformation($"获取已发布事件版本成功！ {stream}");
                    await DispatchEventAsync(processingEvent);
                }
                else if (streamVersion > exceptedVersion)
                {
                    logger.LogInformation($"当前事件版本大于待处理版本！ [CurrentVersion = {currentVersion}, EventStream = {stream}]");
                    processingEvent.EnqueueToWaitingQueue();
                }
                else
                {
                    logger.LogError($"当前事件版本小于待处理版本！ [CurrentVersion = {currentVersion}, EventStream = {stream}]");
                    await processingEvent.OnQueueRejectedAsync();
                }
            }
            else
            {
                logger.LogError(result.Exception, $"获取已发布事件版本失败！ {stream}");
                await processingEvent.OnQueueRejectedAsync();
            }
        }

        #endregion
    }
}
