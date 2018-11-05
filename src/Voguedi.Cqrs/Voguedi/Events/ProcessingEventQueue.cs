using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;

namespace Voguedi.Events
{
    class ProcessingEventQueue : IProcessingEventQueue
    {
        #region Private Fields

        readonly string aggregateRootId;
        readonly IProcessingEventHandler handler;
        readonly ILogger logger;
        readonly BlockingCollection<ProcessingEvent> queue = new BlockingCollection<ProcessingEvent>(new ConcurrentQueue<ProcessingEvent>());
        readonly ConcurrentDictionary<long, ProcessingEvent> waitingQueue = new ConcurrentDictionary<long, ProcessingEvent>();
        readonly object syncLock = new object();
        readonly AsyncLock asyncLock = new AsyncLock();
        const int starting = 1;
        const int stop = 0;
        int isStarting;
        DateTime lastActiveOn;

        #endregion

        #region Ctors

        public ProcessingEventQueue(string aggregateRootId, IProcessingEventHandler handler, ILogger<ProcessingEventQueue> logger)
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
            var processingEvent = default(ProcessingEvent);

            try
            {
                while (!queue.IsCompleted)
                {
                    if (queue.TryTake(out processingEvent) && processingEvent != null)
                        await handler.HandleAsync(processingEvent);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"事件处理队列启动失败！ [AggregateRootId = {aggregateRootId}]");
                Thread.Sleep(1);
            }
            finally
            {
                if (processingEvent == null)
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

        #region IProcessingEventQueue

        public void Enqueue(ProcessingEvent processingEvent)
        {
            lock (syncLock)
            {
                processingEvent.Queue = this;
                queue.TryAdd(processingEvent);
            }

            lastActiveOn = DateTime.UtcNow;
            TryStart();
        }

        public void EnqueueToWaiting(ProcessingEvent processingEvent)
        {
            lock (syncLock)
                waitingQueue.TryAdd(processingEvent.Stream.Version, processingEvent);

            lastActiveOn = DateTime.UtcNow;
            Restart();
        }

        public async Task CommitAsync(ProcessingEvent processingEvent)
        {
            using (await asyncLock.LockAsync())
            {
                lastActiveOn = DateTime.UtcNow;
                var currentVersion = processingEvent.Stream.Version;
                await processingEvent.OnConsumerCommittedAsync();

                if (waitingQueue.TryGetValue(currentVersion + 1, out var next))
                    await handler.HandleAsync(next);
                else
                    Restart();
            }
        }

        public async Task RejectAsync(ProcessingEvent processingEvent)
        {
            using (await asyncLock.LockAsync())
            {
                lastActiveOn = DateTime.UtcNow;
                var currentVersion = processingEvent.Stream.Version;
                await processingEvent.OnConsumerRejectedAsync();

                if (waitingQueue.TryGetValue(currentVersion + 1, out var next))
                    await next.OnConsumerRejectedAsync();
                else
                    Restart();
            }
        }

        public bool IsInactive(int expiration) => (DateTime.UtcNow - lastActiveOn).TotalSeconds >= expiration && isStarting == starting;

        #endregion
    }
}
