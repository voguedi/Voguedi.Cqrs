using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Voguedi.BackgroundWorkers;
using Voguedi.DisposableObjects;
using Voguedi.IdentityGeneration;

namespace Voguedi.Domain.Events
{
    class EventProcessor : DisposableObject, IEventProcessor
    {
        #region Private Fields

        readonly IProcessingEventQueueFactory queueFactory;
        readonly IBackgroundWorker backgroundWorker;
        readonly ILogger logger;
        readonly int expiration;
        readonly string backgroundWorkerKey;
        readonly ConcurrentDictionary<string, IProcessingEventQueue> queueMapping = new ConcurrentDictionary<string, IProcessingEventQueue>();
        bool disposed;
        bool started;

        #endregion

        #region Ctors

        public EventProcessor(
            IProcessingEventQueueFactory queueFactory,
            IBackgroundWorker backgroundWorker,
            IStringIdentityGenerator identityGenerator,
            ILogger<EventProcessor> logger,
            VoguediOptions options)
        {
            this.queueFactory = queueFactory;
            this.backgroundWorker = backgroundWorker;
            this.logger = logger;
            expiration = options.MemoryQueueExpiration;
            backgroundWorkerKey = $"{nameof(EventProcessor)}_{identityGenerator.Generate()}";
        }

        #endregion

        #region DisposableObject

        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                    backgroundWorker.Stop(backgroundWorkerKey);

                disposed = true;
            }
        }

        #endregion

        #region Private Methods

        void Clear()
        {
            var queue = new List<KeyValuePair<string, IProcessingEventQueue>>();

            foreach (var item in queueMapping)
            {
                if (item.Value.IsInactive(expiration))
                    queue.Add(item);
            }

            foreach (var item in queue)
            {
                if (queueMapping.TryRemove(item.Key))
                    logger.LogInformation($"不活跃命令处理队列清理成功！ [AggregateRootId = {item.Key}, QueueActiveExpiration = {expiration}]");
            }
        }

        #endregion

        #region IEventProcessor

        public void Process(ProcessingEvent processingEvent)
        {
            var aggregateRootId = processingEvent.Stream.AggregateRootId;

            if (string.IsNullOrWhiteSpace(aggregateRootId))
                throw new ArgumentException(nameof(processingEvent), $"事件处理的聚合根 Id 不能为空！");

            var queue = queueMapping.GetOrAdd(aggregateRootId, queueFactory.Create);
            queue.Enqueue(processingEvent);
        }

        public void Start()
        {
            if (!started)
            {
                backgroundWorker.Start(backgroundWorkerKey, Clear, expiration, expiration);
                started = true;
            }
        }

        #endregion
    }
}
