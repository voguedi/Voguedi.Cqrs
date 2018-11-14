using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Voguedi.Reflection;
using Voguedi.Utilities;

namespace Voguedi.Messaging
{
    class MessageSubscriptionManager : IMessageSubscriptionManager
    {
        #region Private Class

        class MessageSubscription
        {
            #region Public Properties

            public string QueueTopic { get; }

            public int QueueCount { get; }

            #endregion

            #region Ctors

            public MessageSubscription(string queueTopic, int queueCount)
            {
                QueueTopic = queueTopic;
                QueueCount = queueCount;
            }

            #endregion
        }

        #endregion

        #region Private Fields

        readonly ITypeFinder typeFinder;
        readonly ConcurrentDictionary<string, string> queueMapping = new ConcurrentDictionary<string, string>();
        readonly ConcurrentDictionary<Type, MessageSubscription> subscriptionMapping = new ConcurrentDictionary<Type, MessageSubscription>();

        #endregion

        #region Ctors
        
        public MessageSubscriptionManager(ITypeFinder typeFinder) => this.typeFinder = typeFinder;

        #endregion

        #region Private Methods

        IReadOnlyDictionary<Type, MessageSubscriberAttribute> GetAttributeMapping(Type messageBaseType, string defaultGroupName, int defaultTopicQueueCount)
        {
            var attributes = new Dictionary<Type, MessageSubscriberAttribute>();
            var attribute = default(MessageSubscriberAttribute);

            foreach (var type in typeFinder.GetTypes().Where(t => t.IsClass && !t.IsAbstract && messageBaseType.IsAssignableFrom(t)))
            {
                attribute = type.GetTypeInfo().GetCustomAttribute<MessageSubscriberAttribute>(true);

                if (attribute != null)
                {
                    if (string.IsNullOrWhiteSpace(attribute.Topic))
                        throw new Exception($"订阅者 {type} 订阅主题不能为空！ [MessageBaseType = {messageBaseType}]");

                    if (string.IsNullOrWhiteSpace(attribute.GroupName))
                    {
                        if (string.IsNullOrWhiteSpace(defaultGroupName))
                            throw new Exception($"默认组名不能为空！ [MessageBaseType = {messageBaseType}]");

                        attribute.GroupName = defaultGroupName;
                    }

                    if (attribute.TopicQueueCount <= 0)
                    {
                        if (defaultTopicQueueCount <= 0)
                            throw new Exception($"默认主题队列数量小于 1 ！ [MessageBaseType = {messageBaseType}]");

                        attribute.TopicQueueCount = defaultTopicQueueCount;
                    }

                    attributes.Add(type, attribute);
                }
            }

            return attributes;
        }

        void RegisterQueue(string queueTopic, int queueCount)
        {
            if (queueCount == 1)
                queueMapping.TryAdd(queueTopic, queueTopic);
            else
            {
                for (var i = 0; i < queueCount; i++)
                    queueMapping.TryAdd($"{queueTopic}_{i}", $"{queueTopic}_{i}");
            }
        }

        void RegisterSubscription(Type messageType, string queueTopic, int queueCount) => subscriptionMapping.TryAdd(messageType, new MessageSubscription(queueTopic, queueCount));

        #endregion

        #region IMessageSubscriptionManager

        public IReadOnlyDictionary<string, string> GetQueues() => queueMapping;

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

            throw new Exception($"消息 {message.GetType()} 未标记相关订阅特性！");
        }

        public void Register(Type messageBaseType, string defaultGroupName, int defaultTopicQueueCount)
        {
            var attribute = default(MessageSubscriberAttribute);
            var queueTopic = string.Empty;
            var queueCount = 0;

            foreach (var mapping in GetAttributeMapping(messageBaseType, defaultGroupName, defaultTopicQueueCount))
            {
                attribute = mapping.Value;
                queueTopic = $"{attribute.GroupName}.{attribute.Topic}";
                queueCount = attribute.TopicQueueCount;
                RegisterQueue(queueTopic, queueCount);
                RegisterSubscription(mapping.Key, queueTopic, queueCount);
            }
        }

        #endregion
    }
}
