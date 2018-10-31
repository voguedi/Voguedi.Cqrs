using Voguedi.Messaging;

namespace Voguedi.Domain.Events
{
    public interface IDomainEvent : IMessage
    {
        #region Properties

        string AggregateRootTypeName { get; set; }

        string AggregateRootId { get; set; }

        long Version { get; set; }

        #endregion
    }
}
