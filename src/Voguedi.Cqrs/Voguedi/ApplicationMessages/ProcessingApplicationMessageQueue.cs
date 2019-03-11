using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;

namespace Voguedi.ApplicationMessages
{
    class ProcessingApplicationMessageQueue : IProcessingApplicationMessageQueue
    {
        #region Private Fields

        readonly string routingKey;
        readonly IProcessingApplicationMessageHandler handler;
        readonly ILogger logger;
        readonly BlockingCollection<ProcessingApplicationMessage> queue = new BlockingCollection<ProcessingApplicationMessage>(new ConcurrentQueue<ProcessingApplicationMessage>());
        readonly object syncLock = new object();
        readonly AsyncLock asyncLock = new AsyncLock();
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
            var processingMessage = default(ProcessingApplicationMessage);

            try
            {
                while (!queue.IsCompleted)
                {
                    if (queue.TryTake(out processingMessage) && processingMessage != null)
                        await handler.HandleAsync(processingMessage);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"应用消息处理队列启动失败！ [RoutingKey = {routingKey}]");
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

        public async Task CommitAsync(ProcessingApplicationMessage processingApplicationMessage)
        {
            using (await asyncLock.LockAsync())
            {
                lastActiveOn = DateTime.UtcNow;
                await processingApplicationMessage.OnConsumerCommittedAsync();
            }
        }

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

        public bool IsInactive(int expiration) => (DateTime.UtcNow - lastActiveOn).TotalSeconds >= expiration && isStarting == starting;

        public async Task RejectAsync(ProcessingApplicationMessage processingApplicationMessage)
        {
            using (await asyncLock.LockAsync())
            {
                lastActiveOn = DateTime.UtcNow;
                await processingApplicationMessage.OnConsumerRejectedAsync();
            }
        }

        #endregion
    }
}
