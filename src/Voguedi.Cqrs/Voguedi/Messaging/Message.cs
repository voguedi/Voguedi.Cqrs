using System;
using Voguedi.IdentityGeneration;

namespace Voguedi.Messaging
{
    public abstract class Message : IMessage
    {
        #region Ctors

        protected Message()
        {
            Id = StringIdentityGenerator.Instance.Generate();
            Timestamp = DateTime.UtcNow;
        }

        #endregion

        #region IMessage

        public string Id { get; set; }

        public DateTime Timestamp { get; set; }

        public virtual string GetRoutingKey() => GetType().Name;

        public string GetTypeName() => GetType().FullName;

        #endregion
    }
}
