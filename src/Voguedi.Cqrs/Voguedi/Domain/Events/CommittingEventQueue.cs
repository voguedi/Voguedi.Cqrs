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
        readonly BlockingCollection<CommittingEvent> queue;
        readonly object syncLock;
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
            queue = new BlockingCollection<CommittingEvent>(new ConcurrentQueue<CommittingEvent>());
            syncLock = new object();
            lastActiveOn = DateTime.UtcNow;
        }

        #endregion

        #region Private Methods

        void TryStart()
        {
            if (Interlocked.CompareExchange(ref isStarting, starting, stop) == stop)
                Task.Factory.StartNew(StartAsync);
        }

        async Task StartAsync()
        {
            lastActiveOn = DateTime.UtcNow;
            var committingEvent = default(CommittingEvent);

            try
            {
                while (!queue.IsCompleted && queue.TryTake(out committingEvent))
                    await handler.HandleAsync(committingEvent);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"队列启动失败。 [AggregateRootId = {aggregateRootId}]");
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

        void Restart()
        {
            Stop();
            TryStart();
        }

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

        public Task CommitAsync()
        {
            Restart();
            return Task.CompletedTask;
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
