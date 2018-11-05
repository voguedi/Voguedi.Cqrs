using Voguedi.Messaging;

namespace Voguedi.Events
{
    public abstract class Event<TIdentity> : Message, IEvent<TIdentity>
    {
        #region Message

        public override string GetRoutingKey() => AggregateRootId?.ToString();

        #endregion

        #region IEvent<TIdentity>

        public TIdentity AggregateRootId { get; set; }

        public long Version { get; set; }

        #endregion
    }
}
