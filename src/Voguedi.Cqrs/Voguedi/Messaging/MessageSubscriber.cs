using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Voguedi.DisposableObjects;
using Voguedi.Reflection;

namespace Voguedi.Messaging
{
    public abstract class MessageSubscriber : DisposableObject, IMessageSubscriber
    {
        #region Private Fields

        readonly IMessageConsumerFactory consumerFactory;
        readonly ITypeFinder typeFinder;
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

        protected MessageSubscriber(IMessageConsumerFactory consumerFactory, ITypeFinder typeFinder, ILogger logger, string defaultGroupName, int defaultTopicQueueCount)
        {
            this.consumerFactory = consumerFactory;
            this.typeFinder = typeFinder;
            this.logger = logger;
            this.defaultGroupName = defaultGroupName;
            this.defaultTopicQueueCount = defaultTopicQueueCount;
        }

        #endregion

        #region Private Methods

        IReadOnlyList<MessageSubscriberAttribute> GetAttributes()
        {
            var attributes = new List<MessageSubscriberAttribute>();
            var attribute = default(MessageSubscriberAttribute);
            var baseType = GetSubscriberBaseType();

            foreach (var type in typeFinder.GetTypes())
            {
                if (type.IsClass && !type.IsAbstract && baseType.IsAssignableFrom(type))
                {
                    attribute = type.GetTypeInfo().GetCustomAttribute<MessageSubscriberAttribute>(true);

                    if (attribute != null)
                        attributes.Add(attribute);
                }
            }

            return attributes;
        }

        IReadOnlyList<string> GetQueues()
        {
            var queues = new List<string>();

            foreach (var attribute in GetAttributes())
            {
                if (string.IsNullOrWhiteSpace(attribute.GroupName))
                    attribute.GroupName = defaultGroupName;

                if (attribute.TopicQueueCount <= 0)
                    attribute.TopicQueueCount = defaultTopicQueueCount;

                if (attribute.TopicQueueCount == 1)
                    queues.Add($"{attribute.GroupName}.{attribute.Topic}");
                else
                    queues.AddRange(BuildQueues(attribute.GroupName, attribute.Topic, attribute.TopicQueueCount));
            }

            return queues.Distinct().ToList();
        }

        IReadOnlyList<string> BuildQueues(string groupName, string topic, int topicQueueCount)
        {
            var queues = new List<string>();

            for (var i = 0; i < topicQueueCount; i++)
                queues.Add($"{groupName}.{topic}.{i}");

            return queues;
        }

        void RegisterProcessor(IMessageConsumer consumer) => consumer.Received += (sender, e) => Process(e, consumer);

        #endregion

        #region Protected Methods

        protected abstract Type GetSubscriberBaseType();

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
                        logger.LogError(ex, "消息订阅器释放异常！");
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
                foreach (var queue in GetQueues())
                {
                    Task.Factory.StartNew(
                        () =>
                        {
                            using (var consumer = consumerFactory.Create(queue))
                            {
                                RegisterProcessor(consumer);
                                consumer.Subscribe(queue);
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
