using System;

namespace Voguedi.Messaging
{
    [AttributeUsage(AttributeTargets.Class)]
    public abstract class MessageSubscriberAttribute : Attribute
    {
        #region Public Properties

        public string Topic { get; }

        public string GroupName { get; set; }

        public int TopicQueueCount { get; set; }

        #endregion

        #region Ctors

        protected MessageSubscriberAttribute(string topic) => Topic = topic;

        #endregion
    }
}
