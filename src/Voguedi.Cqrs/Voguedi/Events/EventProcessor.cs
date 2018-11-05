using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Voguedi.ActiveCheckers;
using Voguedi.DisposableObjects;

namespace Voguedi.Events
{
    class EventProcessor : DisposableObject, IEventProcessor
    {
        #region Private Fields

        readonly IProcessingEventQueueFactory queueFactory;
        readonly IMemoryQueueActiveChecker queueActiveChecker;
        readonly ILogger logger;
        readonly int queueActiveExpiration;
        readonly ConcurrentDictionary<string, IProcessingEventQueue> queueMapping = new ConcurrentDictionary<string, IProcessingEventQueue>();
        bool disposed;
        bool started;

        #endregion

        #region Ctors

        public EventProcessor(IProcessingEventQueueFactory queueFactory, IMemoryQueueActiveChecker queueActiveChecker, ILogger<EventProcessor> logger, VoguediOptions options)
        {
            this.queueFactory = queueFactory;
            this.queueActiveChecker = queueActiveChecker;
            this.logger = logger;
            queueActiveExpiration = options.MemoryQueueActiveExpiration;
        }

        #endregion

        #region DisposableObject

        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                    queueActiveChecker.Stop(nameof(EventProcessor));

                disposed = true;
            }
        }

        #endregion

        #region Private Methods

        void ClearInactiveQueue()
        {
            var queue = new List<KeyValuePair<string, IProcessingEventQueue>>();

            foreach (var item in queueMapping)
            {
                if (item.Value.IsInactive(queueActiveExpiration))
                    queue.Add(item);
            }

            foreach (var item in queue)
            {
                if (queueMapping.TryRemove(item.Key))
                    logger.LogInformation($"不活跃命令处理队列清理成功！ [AggregateRootId = {item.Key}, QueueActiveExpiration = {queueActiveExpiration}]");
            }
        }

        #endregion

        #region IEventProcessor

        public Task ProcessAsync(ProcessingEvent processingEvent)
        {
            var aggregateRootId = processingEvent.Stream.AggregateRootId;

            if (string.IsNullOrWhiteSpace(aggregateRootId))
                throw new ArgumentException(nameof(processingEvent), $"处理事件的聚合根 Id 不能为空！");

            var queue = queueMapping.GetOrAdd(aggregateRootId, key => queueFactory.Create(key));
            queue.Enqueue(processingEvent);
            return Task.CompletedTask;
        }

        public void Start()
        {
            if (!started)
            {
                queueActiveChecker.Start(nameof(EventProcessor), ClearInactiveQueue, queueActiveExpiration);
                started = true;
            }
        }

        #endregion
    }
}
