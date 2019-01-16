using System;
using Voguedi.Utils;

namespace Voguedi.Messaging
{
    public abstract class Message : IMessage
    {
        #region Ctors

        protected Message()
        {
            Id = SnowflakeId.Instance.NewId();
            Timestamp = DateTime.UtcNow;
        }

        #endregion

        #region IMessage

        public long Id { get; set; }

        public DateTime Timestamp { get; set; }

        public virtual string GetRoutingKey() => GetType().Name;

        #endregion
    }
}
