using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Voguedi.BackgroundWorkers;
using Voguedi.Domain.Caching;
using Voguedi.Infrastructure;

namespace Voguedi.Domain.Events
{
    class EventCommitter : IEventCommitter
    {
        #region Private Fields

        readonly ICommittingEventQueueFactory queueFactory;
        readonly ICache cache;
        readonly IBackgroundWorker backgroundWorker;
        readonly ILogger logger;
        readonly int expiration;
        readonly string backgroundWorkerKey;
        readonly ConcurrentDictionary<string, ICommittingEventQueue> queueMapping;
        bool started;
        bool stopped;

        #endregion

        #region Ctors

        public EventCommitter(ICommittingEventQueueFactory queueFactory, ICache cache, IBackgroundWorker backgroundWorker, ILogger<EventCommitter> logger, VoguediOptions options)
        {
            this.queueFactory = queueFactory;
            this.cache = cache;
            this.backgroundWorker = backgroundWorker;
            this.logger = logger;
            expiration = options.MemoryQueueExpiration;
            backgroundWorkerKey = $"{nameof(EventCommitter)}_{SnowflakeId.Default().NewId()}";
            queueMapping = new ConcurrentDictionary<string, ICommittingEventQueue>();
        }

        #endregion

        #region Private Methods

        Task SetAggregateRootCache(CommittingEvent committingEvent)
        {
            var stream = committingEvent.Stream;
            var aggregateRoot = committingEvent.AggregateRoot;
            aggregateRoot.CommitEvents(stream.Version);
            return cache.SetAsync(aggregateRoot);
        }

        void Clear()
        {
            var queue = new List<KeyValuePair<string, ICommittingEventQueue>>();

            foreach (var item in queueMapping)
            {
                if (item.Value.IsInactive(expiration))
                    queue.Add(item);
            }

            foreach (var item in queue)
            {
                if (queueMapping.TryRemove(item.Key))
                    logger.LogDebug($"已过期队列清理成功。 [AggregateRootId = {item.Key}, Expiration = {expiration}]");
            }
        }

        #endregion

        #region IEventCommitter

        public Task CommitAsync(CommittingEvent committingEvent)
        {
            queueMapping
                .GetOrAdd(committingEvent.ProcessingCommand.Command.AggregateRootId, queueFactory.Create)
                .Enqueue(committingEvent);
            return SetAggregateRootCache(committingEvent);
        }

        public void Start()
        {
            if (!started)
            {
                backgroundWorker.Start(backgroundWorkerKey, Clear, expiration, expiration);
                started = true;
            }
        }

        public void Stop()
        {
            if (!stopped)
            {
                backgroundWorker.Stop(backgroundWorkerKey);
                stopped = true;
            }
        }

        #endregion
    }
}
