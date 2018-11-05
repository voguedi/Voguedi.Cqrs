using Voguedi.Messaging;

namespace Voguedi.Events
{
    public interface IEvent : IMessage
    {
        #region Properties

        string AggregateRootTypeName { get; set; }

        string AggregateRootId { get; set; }

        long Version { get; set; }

        #endregion
    }
}
