using System;
using System.Collections.Generic;

namespace Voguedi.Messaging
{
    public interface IMessageSubscriptionManager
    {
        #region Public Methods

        void Register(Type messageBaseType, string defaultGroupName, int defaultTopicQueueCount);

        IReadOnlyDictionary<string, string> GetQueues(Type messageBaseType);

        string GetQueueTopic(IMessage message);

        #endregion
    }
}
