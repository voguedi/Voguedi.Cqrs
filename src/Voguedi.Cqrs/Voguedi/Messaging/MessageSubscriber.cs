using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Voguedi.DisposableObjects;

namespace Voguedi.Messaging
{
    public abstract class MessageSubscriber : DisposableObject, IMessageSubscriber
    {
        #region Private Fields

        readonly IMessageConsumerFactory consumerFactory;
        readonly IMessageSubscriptionManager subscriptionManager;
        readonly ILogger logger;
        readonly string defaultGroupName;
        readonly int defaultTopicQueueCount;
        readonly TimeSpan timeout = TimeSpan.FromSeconds(1);
        readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        bool disposed;
        Task startedTask;
        bool started;

        #endregion

        #region Ctors

        protected MessageSubscriber(
            IMessageConsumerFactory consumerFactory,
            IMessageSubscriptionManager subscriptionManager,
            ILogger logger,
            string defaultGroupName,
            int defaultTopicQueueCount)
        {
            this.consumerFactory = consumerFactory;
            this.subscriptionManager = subscriptionManager;
            this.logger = logger;
            this.defaultGroupName = defaultGroupName;
            this.defaultTopicQueueCount = defaultTopicQueueCount;
        }

        #endregion

        #region Private Methods

        void RegisterProcessor(IMessageConsumer consumer) => consumer.Received += (sender, e) => Process(e, consumer);

        #endregion

        #region Protected Methods

        protected abstract Type GetMessageBaseType();

        protected abstract void Process(ReceivingMessage receivingMessage, IMessageConsumer consumer);

        #endregion

        #region DisposableObject

        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    cancellationTokenSource.Cancel();

                    try
                    {
                        startedTask.Wait(TimeSpan.FromSeconds(2));
                    }
                    catch (OperationCanceledException ex)
                    {
                        logger.LogError(ex, "订阅器当前状态异常！");
                    }
                }

                disposed = true;
            }
        }

        #endregion

        #region IMessageSubscriber

        public void Start()
        {
            if (!started)
            {
                subscriptionManager.Register(GetMessageBaseType(), defaultGroupName, defaultTopicQueueCount);

                foreach (var queue in subscriptionManager.GetQueues())
                {
                    Task.Factory.StartNew(
                        () =>
                        {
                            using (var consumer = consumerFactory.Create(queue.Key))
                            {
                                RegisterProcessor(consumer);
                                consumer.Subscribe(queue.Value);
                                consumer.Listening(timeout, cancellationTokenSource.Token);
                            }
                        },
                        cancellationTokenSource.Token,
                        TaskCreationOptions.LongRunning,
                        TaskScheduler.Default);
                }

                startedTask = Task.CompletedTask;
                started = true;
            }
        }

        #endregion
    }
}
