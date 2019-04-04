using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Voguedi.Reflection;
using Voguedi.Infrastructure;

namespace Voguedi.Messaging
{
    class MessageSubscriptionManager : IMessageSubscriptionManager
    {
        #region Private Class

        class MessageSubscription
        {
            #region Ctors

            public MessageSubscription(string queueTopic, int queueCount)
            {
                QueueTopic = queueTopic;
                QueueCount = queueCount;
            }

            #endregion

            #region Public Properties

            public string QueueTopic { get; }

            public int QueueCount { get; }

            #endregion
        }

        #endregion

        #region Private Fields

        readonly ITypeFinder typeFinder;
        readonly Assembly[] assemblies;
        readonly ConcurrentDictionary<Type, Dictionary<string, string>> queueMapping;
        readonly ConcurrentDictionary<Type, MessageSubscription> subscriptionMapping;

        #endregion

        #region Ctors
        
        public MessageSubscriptionManager(ITypeFinder typeFinder, VoguediOptions options)
        {
            this.typeFinder = typeFinder;
            assemblies = options.Assemblies;
            queueMapping = new ConcurrentDictionary<Type, Dictionary<string, string>>();
            subscriptionMapping = new ConcurrentDictionary<Type, MessageSubscription>();
        }

        #endregion

        #region Private Methods

        IReadOnlyDictionary<Type, MessageSubscriberAttribute> GetAttributeMapping(Type messageType, string defaultGroupName, int defaultTopicQueueCount)
        {
            var attributes = new Dictionary<Type, MessageSubscriberAttribute>();

            foreach (var type in typeFinder.GetTypesBySpecifiedType(messageType, assemblies))
            {
                var attribute = type.GetTypeInfo().GetCustomAttribute<MessageSubscriberAttribute>(true);

                if (attribute != null)
                {
                    if (string.IsNullOrWhiteSpace(attribute.Topic))
                        throw new Exception($"订阅者主题不能为空。 [SubscriberType = {type}]");

                    if (string.IsNullOrWhiteSpace(attribute.GroupName) && !string.IsNullOrWhiteSpace(defaultGroupName))
                        attribute.GroupName = defaultGroupName;

                    if (attribute.TopicQueueCount <= 0)
                    {
                        if (defaultTopicQueueCount <= 0)
                            throw new Exception($"默认主题队列数量小于 1 。");

                        attribute.TopicQueueCount = defaultTopicQueueCount;
                    }

                    attributes.Add(type, attribute);
                }
            }

            return attributes;
        }

        void RegisterQueue(Type messageType, string queueTopic, int queueCount)
        {
            var queues = new List<string>();

            if (queueCount == 1)
                queues.Add(queueTopic);
            else
            {
                for (var i = 0; i < queueCount; i++)
                    queues.Add($"{queueTopic}_{i}");
            }

            if (!queueMapping.TryGetValue(messageType, out var mapping))
                mapping = new Dictionary<string, string>();

            foreach (var queue in queues)
            {
                if (!mapping.ContainsKey(queue))
                    mapping[queue] = queue;
            }

            queueMapping[messageType] = mapping;
        }

        void RegisterSubscription(Type messageType, string queueTopic, int queueCount) => subscriptionMapping.TryAdd(messageType, new MessageSubscription(queueTopic, queueCount));

        #endregion

        #region IMessageSubscriptionManager

        public IReadOnlyDictionary<string, string> GetQueues(Type messageType) => queueMapping[messageType];

        public string GetQueueTopic(IMessage message)
        {
            if (subscriptionMapping.TryGetValue(message.GetType(), out var subscription))
            {
                if (subscription.QueueCount == 1)
                    return subscription.QueueTopic;

                var routingKey = message.GetRoutingKey();
                var queueIndex = Utils.GetHashCode(routingKey) % subscription.QueueCount;
                return $"{subscription.QueueTopic}_{queueIndex}";
            }

            throw new Exception($"订阅者未标记相关订阅特性。 [SubscriberType = {message.GetType()}]");
        }

        public void Register(Type messageType, string defaultGroupName, int defaultTopicQueueCount)
        {
            foreach (var mapping in GetAttributeMapping(messageType, defaultGroupName, defaultTopicQueueCount))
            {
                var attribute = mapping.Value;
                var queueTopic = $"{attribute.GroupName}.{attribute.Topic}";
                var queueCount = attribute.TopicQueueCount;
                RegisterQueue(messageType, queueTopic, queueCount);
                RegisterSubscription(mapping.Key, queueTopic, queueCount);
            }
        }

        #endregion
    }
}
