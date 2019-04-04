using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Voguedi.Infrastructure;
using Voguedi.MessageQueues;

namespace Voguedi.Messaging
{
    public abstract class MessageSubscriber<TMessage> : DisposableObject, IMessageSubscriber
        where TMessage : class, IMessage
    {
        #region Private Fields

        readonly IMessageQueueConsumerFactory queueConsumerFactory;
        readonly IMessageSubscriptionManager subscriptionManager;
        readonly IMessageProcessor processor;
        readonly ILogger logger;
        readonly string defaultGroupName;
        readonly int defaultTopicQueueCount;
        readonly TimeSpan timeout;
        readonly CancellationTokenSource cancellationTokenSource;
        bool disposed;
        Task startedTask;
        bool started;

        #endregion

        #region Ctors

        protected MessageSubscriber(
            IMessageQueueConsumerFactory consumerFactory,
            IMessageSubscriptionManager subscriptionManager,
            IMessageProcessor processor,
            ILogger logger,
            string defaultGroupName,
            int defaultTopicQueueCount)
        {
            this.queueConsumerFactory = consumerFactory;
            this.subscriptionManager = subscriptionManager;
            this.processor = processor;
            this.logger = logger;
            this.defaultGroupName = defaultGroupName;
            this.defaultTopicQueueCount = defaultTopicQueueCount;
            timeout = TimeSpan.FromSeconds(1);
            cancellationTokenSource = new CancellationTokenSource();
        }

        #endregion

        #region Private Methods

        void RegisterProcessor(IMessageQueueConsumer queueConsumer)
        {
            queueConsumer.Received += (sender, e) =>
            {
                logger.LogDebug($"消息接收成功，开始处理消息。 {e}");

                try
                {
                    processor.Process(e.QueueMessage);
                    queueConsumer.Commit();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"已接收消息处理失败。 {e}");
                    queueConsumer.Reject();
                }
            };
        }

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
                        logger.LogError(ex, "操作取消。");
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
                var messageType = typeof(TMessage);
                subscriptionManager.Register(messageType, defaultGroupName, defaultTopicQueueCount);

                foreach (var queue in subscriptionManager.GetQueues(messageType))
                {
                    Task.Factory.StartNew(
                        () =>
                        {
                            using (var consumer = queueConsumerFactory.Create(queue.Key))
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
