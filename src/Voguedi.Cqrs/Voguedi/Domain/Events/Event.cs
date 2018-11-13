using Voguedi.Messaging;

namespace Voguedi.Domain.Events
{
    public abstract class Event<TIdentity> : Message, IEvent<TIdentity>
    {
        #region Ctors

        protected Event() : base() { }

        #endregion

        #region Message

        public override string GetRoutingKey() => AggregateRootId?.ToString();

        #endregion

        #region IEvent<TIdentity>

        public TIdentity AggregateRootId { get; set; }

        public long Version { get; set; }

        #endregion
    }
}
