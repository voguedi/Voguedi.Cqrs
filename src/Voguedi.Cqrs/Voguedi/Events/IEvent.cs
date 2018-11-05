using Voguedi.Messaging;

namespace Voguedi.Events
{
    public interface IEvent : IMessage
    {
        #region Properties

        long Version { get; set; }

        #endregion
    }

    public interface IEvent<TIdentity> : IEvent
    {
        #region Properties

        TIdentity AggregateRootId { get; set; }

        #endregion
    }
}
