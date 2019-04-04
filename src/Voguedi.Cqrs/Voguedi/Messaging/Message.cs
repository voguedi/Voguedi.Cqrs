using System;
using Voguedi.Infrastructure;

namespace Voguedi.Messaging
{
    public abstract class Message : IMessage
    {
        #region Ctors

        protected Message()
        {
            Id = SnowflakeId.Default().NewId();
            Timestamp = DateTime.UtcNow;
        }

        #endregion

        #region IMessage

        public long Id { get; set; }

        public DateTime Timestamp { get; set; }

        public virtual string GetRoutingKey() => GetType().Name;

        public string GetTag() => GetType().AssemblyQualifiedName;

        #endregion
    }
}
