using System;
using System.Collections.Generic;

namespace Voguedi.Messaging
{
    public interface IMessageSubscriptionManager
    {
        #region Public Methods

        void Register(Type messageType, string defaultGroupName, int defaultTopicQueueCount);

        IReadOnlyDictionary<string, string> GetQueues(Type messageType);

        string GetQueueTopic(IMessage message);

        #endregion
    }
}
