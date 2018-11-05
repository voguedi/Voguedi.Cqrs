using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Voguedi.Schedulers;
using Voguedi.DisposableObjects;

namespace Voguedi.Events
{
    class EventCommitter : DisposableObject, IEventCommitter
    {
        #region Private Fields

        readonly ICommittingEventQueueFactory queueFactory;
        readonly IScheduler scheduler;
        readonly ILogger logger;
        readonly int queueActiveExpiration;
        readonly ConcurrentDictionary<string, ICommittingEventQueue> queueMapping = new ConcurrentDictionary<string, ICommittingEventQueue>();
        bool disposed;
        bool started;

        #endregion

        #region Ctors

        public EventCommitter(ICommittingEventQueueFactory queueFactory, IScheduler scheduler, ILogger<EventCommitter> logger, VoguediOptions options)
        {
            this.queueFactory = queueFactory;
            this.scheduler = scheduler;
            this.logger = logger;
            queueActiveExpiration = options.MemoryQueueActiveExpiration;
        }

        #endregion

        #region Private Methods

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
                    scheduler.Stop(nameof(EventCommitter));

                disposed = true;
            }
        }

        #endregion

        #region IEventCommitter

        public Task CommitAsync(CommittingEvent committingEvent)
        {
            var aggregateRootId = committingEvent.ProcessingCommand.Command.AggregateRootId;

            if (string.IsNullOrWhiteSpace(aggregateRootId))
                throw new ArgumentException(nameof(committingEvent), $"提交事件的聚合根 Id 不能为空！");

            var queue = queueMapping.GetOrAdd(aggregateRootId, queueFactory.Create);
            queue.Enqueue(committingEvent);
            return Task.CompletedTask;
        }

        public void Start()
        {
            if (!started)
            {
                scheduler.Start(nameof(EventCommitter), ClearInactiveQueue, queueActiveExpiration, queueActiveExpiration);
                started = true;
            }
        }

        #endregion
    }
}
