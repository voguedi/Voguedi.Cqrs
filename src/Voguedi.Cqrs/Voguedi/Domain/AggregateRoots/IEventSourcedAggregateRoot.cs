using System.Collections.Generic;
using Voguedi.Domain.Events;

namespace Voguedi.Domain.AggregateRoots
{
    public interface IEventSourcedAggregateRoot
    {
        #region Methods

        IReadOnlyList<IDomainEvent> GetUncommittedEvents();

        void CommitEvents(long committedVersion);

        void ReplayEvents(IReadOnlyList<DomainEventStream> eventStreams);

        string GetId();

        string GetTypeName();

        long GetVersion();

        #endregion
    }
}
