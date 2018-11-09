using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Voguedi.BackgroundWorkers;
using Voguedi.DisposableObjects;
using Voguedi.Domain.Caching;
using Voguedi.IdentityGeneration;

namespace Voguedi.Domain.Events
{
    class EventCommitter : DisposableObject, IEventCommitter
    {
        #region Private Fields

        readonly ICommittingEventQueueFactory queueFactory;
        readonly ICache cache;
        readonly IBackgroundWorker backgroundWorker;
        readonly ILogger logger;
        readonly int queueActiveExpiration;
        readonly string backgroundWorkerKey;
        readonly ConcurrentDictionary<string, ICommittingEventQueue> queueMapping = new ConcurrentDictionary<string, ICommittingEventQueue>();
        bool disposed;
        bool started;

        #endregion

        #region Ctors

        public EventCommitter(ICommittingEventQueueFactory queueFactory, ICache cache, IBackgroundWorker backgroundWorker, ILogger<EventCommitter> logger, VoguediOptions options)
        {
            this.queueFactory = queueFactory;
            this.cache = cache;
            this.backgroundWorker = backgroundWorker;
            this.logger = logger;
            queueActiveExpiration = options.MemoryQueueActiveExpiration;
            backgroundWorkerKey = $"{nameof(EventCommitter)}-{StringIdentityGenerator.Instance.Generate()}";
        }

        #endregion

        #region Private Methods

        async Task SetAggregateRootCache(CommittingEvent committingEvent)
        {
            var stream = committingEvent.Stream;
            var aggregateRoot = committingEvent.AggregateRoot;
            aggregateRoot.CommitEvents(stream.Version);
            var result = await cache.SetAsync(aggregateRoot);

            if (result.Succeeded)
                logger.LogInformation($"事件处理的聚合根缓存更新成功！ [AggregateRootType = {aggregateRoot.GetAggregateRootType()}, AggregateRootId = {aggregateRoot.GetAggregateRootId()}]");
            else
                logger.LogError(result.Exception, $"事件处理的聚合根缓存更新失败！ [AggregateRootType = {aggregateRoot.GetAggregateRootType()}, AggregateRootId = {aggregateRoot.GetAggregateRootId()}]");
        }

        void ClearInactiveQueue()
        {
            var queue = new List<KeyValuePair<string, ICommittingEventQueue>>();

            foreach (var item in queueMapping)
            {
                if (item.Value.IsInactive(queueActiveExpiration))
                    queue.Add(item);
            }

            foreach (var item in queue)
            {
                if (queueMapping.TryRemove(item.Key))
                    logger.LogInformation($"不活跃事件提交队列清理成功！ [AggregateRootId = {item.Key}, QueueActiveExpiration = {queueActiveExpiration}]");
            }
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

        #region IEventCommitter

        public Task CommitAsync(CommittingEvent committingEvent)
        {
            var aggregateRootId = committingEvent.ProcessingCommand.Command.AggregateRootId;

            if (string.IsNullOrWhiteSpace(aggregateRootId))
                throw new ArgumentException(nameof(committingEvent), $"事件处理的聚合根 Id 不能为空！");

            var queue = queueMapping.GetOrAdd(aggregateRootId, queueFactory.Create);
            queue.Enqueue(committingEvent);
            return SetAggregateRootCache(committingEvent);
        }

        public void Start()
        {
            if (!started)
            {
                backgroundWorker.Start(backgroundWorkerKey, ClearInactiveQueue, queueActiveExpiration, queueActiveExpiration);
                started = true;
            }
        }

        #endregion
    }
}
