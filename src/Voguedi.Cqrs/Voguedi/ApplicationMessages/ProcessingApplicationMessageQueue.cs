using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Voguedi.ApplicationMessages
{
    class ProcessingApplicationMessageQueue : IProcessingApplicationMessageQueue
    {
        #region Private Fields

        readonly string routingKey;
        readonly IProcessingApplicationMessageHandler handler;
        readonly ILogger logger;
        readonly BlockingCollection<ProcessingApplicationMessage> queue;
        readonly object syncLock;
        const int starting = 1;
        const int stop = 0;
        int isStarting;
        DateTime lastActiveOn;

        #endregion

        #region Ctors

        public ProcessingApplicationMessageQueue(string routingKey, IProcessingApplicationMessageHandler handler, ILogger<ProcessingApplicationMessageQueue> logger)
        {
            this.routingKey = routingKey;
            this.handler = handler;
            this.logger = logger;
            queue = new BlockingCollection<ProcessingApplicationMessage>(new ConcurrentQueue<ProcessingApplicationMessage>());
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
            var processingMessage = default(ProcessingApplicationMessage);

            try
            {
                while (!queue.IsCompleted && queue.TryTake(out processingMessage))
                    await handler.HandleAsync(processingMessage);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"队列启动失败。 [RoutingKey = {routingKey}]");
                Thread.Sleep(1);
            }
            finally
            {
                if (processingMessage == null)
                {
                    Stop();

                    if (queue.Count > 0)
                        TryStart();
                }
            }
        }

        void Stop() => Interlocked.Exchange(ref isStarting, stop);

        #endregion

        #region IProcessingApplicationMessageQueue

        public void Enqueue(ProcessingApplicationMessage processingApplicationMessage)
        {
            lock (syncLock)
            {
                processingApplicationMessage.Queue = this;
                queue.TryAdd(processingApplicationMessage);
            }

            lastActiveOn = DateTime.UtcNow;
            TryStart();
        }

        public Task ProcessAsync()
        {
            lastActiveOn = DateTime.UtcNow;
            return Task.CompletedTask;
        }

        public bool IsInactive(int expiration) => (DateTime.UtcNow - lastActiveOn).TotalSeconds >= expiration && isStarting == starting;

        #endregion
    }
}
