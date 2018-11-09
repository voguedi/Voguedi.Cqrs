using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Voguedi.Domain.Events
{
    class CommittingEventQueue : ICommittingEventQueue
    {
        #region Private Fields

        readonly string aggregateRootId;
        readonly ICommittingEventHandler handler;
        readonly ILogger logger;
        readonly BlockingCollection<CommittingEvent> queue = new BlockingCollection<CommittingEvent>(new ConcurrentQueue<CommittingEvent>());
        readonly object syncLock = new object();
        const int starting = 1;
        const int stop = 0;
        int isStarting;
        DateTime lastActiveOn;

        #endregion

        #region Ctors

        public CommittingEventQueue(string aggregateRootId, ICommittingEventHandler handler, ILogger<CommittingEventQueue> logger)
        {
            this.aggregateRootId = aggregateRootId;
            this.handler = handler;
            this.logger = logger;
            lastActiveOn = DateTime.UtcNow;
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
            lastActiveOn = DateTime.UtcNow;
            var committingEvent = default(CommittingEvent);

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
                logger.LogError(ex, $"事件提交队列启动失败！ [AggregateRootId = {aggregateRootId}]");
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

        void Stop() => Interlocked.Exchange(ref isStarting, stop);

        #endregion

        #region ICommittingEventQueue

        public void Enqueue(CommittingEvent committingEvent)
        {
            lock (syncLock)
            {
                committingEvent.Queue = this;
                queue.TryAdd(committingEvent);
            }

            lastActiveOn = DateTime.UtcNow;
            TryStart();
        }

        public void Clear()
        {
            queue.Clear();
            Stop();
        }

        public bool IsInactive(int expiration) => (DateTime.UtcNow - lastActiveOn).TotalSeconds >= expiration && isStarting == starting;

        #endregion
    }
}
