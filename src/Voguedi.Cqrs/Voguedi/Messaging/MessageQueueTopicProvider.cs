using System;
using System.Collections.Concurrent;
using System.Reflection;
using Voguedi.Utilities;

namespace Voguedi.Messaging
{
    class MessageQueueTopicProvider : IMessageQueueTopicProvider
    {
        #region Private Fields

        readonly ConcurrentDictionary<Type, string> queueTopicMapping = new ConcurrentDictionary<Type, string>();

        #endregion

        #region IMessageQueueTopicProvider

        public string Get(IMessage message, string defaultGroupName, int defaultTopicQueueCount)
        {
            return queueTopicMapping.GetOrAdd(
                message.GetType(),
                key =>
                {
                    var attribute = key.GetTypeInfo().GetCustomAttribute<MessageSubscriberAttribute>(true);

                    if (attribute == null)
                        throw new ArgumentException(nameof(message), $"{key} 未标记订阅者特性！");

                    if (string.IsNullOrWhiteSpace(attribute.GroupName))
                        attribute.GroupName = defaultGroupName;

                    if (attribute.TopicQueueCount <= 0)
                        attribute.TopicQueueCount = defaultTopicQueueCount;

                    if (attribute.TopicQueueCount > 1)
                        return $"{attribute.GroupName}.{attribute.Topic}.{Utils.GetHashCode(message.GetRoutingKey()) % attribute.TopicQueueCount}";

                    return $"{attribute.GroupName}.{attribute.Topic}";
                });
        }

        #endregion
    }
}
