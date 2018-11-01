using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Voguedi.Domain.Events
{
    class CommittingDomainEventQueue : ICommittingDomainEventQueue
    {
        #region Private Fields

        readonly string aggregateRootId;
        readonly ICommittingDomainEventHandler handler;
        readonly ILogger logger;
        readonly BlockingCollection<CommittingDomainEvent> queue = new BlockingCollection<CommittingDomainEvent>(new ConcurrentQueue<CommittingDomainEvent>());
        readonly object syncLock = new object();
        const int starting = 1;
        const int stop = 0;
        int isStarting;

        #endregion

        #region Ctors

        public CommittingDomainEventQueue(string aggregateRootId, ICommittingDomainEventHandler handler, ILogger<CommittingDomainEventQueue> logger)
        {
            this.aggregateRootId = aggregateRootId;
            this.handler = handler;
            this.logger = logger;
        }

        #endregion

        #region Private Methods

        void TryStart()
        {
            if (Interlocked.CompareExchange(ref isStarting, starting, stop) == stop)
                Task.Factory.StartNew(async () => await StartAsync());
        }

        async Task StartAsync()
        {
            var committingEvent = default(CommittingDomainEvent);

            try
            {
                while (!queue.IsCompleted)
                {
                    if (queue.TryTake(out committingEvent) && committingEvent != null)
                        await handler.HandleAsync(committingEvent);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"领域事件提交队列启动失败！ [AggregateRootId = {aggregateRootId}]");
                Thread.Sleep(1);
            }
            finally
            {
                if (committingEvent == null)
                {
                    Stop();

                    if (queue.Count > 0)
                        TryStart();
                }
            }
        }

        #endregion

        #region ICommittingDomainEventQueue

        public void Enqueue(CommittingDomainEvent committingEvent)
        {
            lock (syncLock)
            {
                committingEvent.Queue = this;
                queue.TryAdd(committingEvent);
            }

            TryStart();
        }

        public void Clear() => queue.Clear();

        public void Stop() => Interlocked.Exchange(ref isStarting, stop);

        #endregion
    }
}
