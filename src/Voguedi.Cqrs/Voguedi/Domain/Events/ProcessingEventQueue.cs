using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Voguedi.Domain.Events
{
    class ProcessingEventQueue : IProcessingEventQueue
    {
        #region Private Fields

        readonly string aggregateRootId;
        readonly IProcessingEventHandler handler;
        readonly ILogger logger;
        readonly BlockingCollection<ProcessingEvent> queue;
        readonly ConcurrentDictionary<long, ProcessingEvent> waitingQueue;
        readonly object syncLock;
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
            queue = new BlockingCollection<ProcessingEvent>(new ConcurrentQueue<ProcessingEvent>());
            waitingQueue = new ConcurrentDictionary<long, ProcessingEvent>();
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
            var processingEvent = default(ProcessingEvent);

            try
            {
                while (!queue.IsCompleted && queue.TryTake(out processingEvent))
                    await handler.HandleAsync(processingEvent);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"队列启动失败。 [AggregateRootId = {aggregateRootId}]");
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

        public async Task ProcessAsync(ProcessingEvent processingEvent)
        {
            lastActiveOn = DateTime.UtcNow;

            if (waitingQueue.TryGetValue(processingEvent.Stream.Version + 1, out var waiting))
                await handler.HandleAsync(waiting);
            else
                Restart();
        }

        public bool IsInactive(int expiration) => (DateTime.UtcNow - lastActiveOn).TotalSeconds >= expiration && isStarting == starting;

        #endregion
    }
}
