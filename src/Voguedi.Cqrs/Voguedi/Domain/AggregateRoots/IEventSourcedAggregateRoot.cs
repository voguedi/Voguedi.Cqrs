using System.Collections.Generic;
using Voguedi.Events;

namespace Voguedi.Domain.AggregateRoots
{
    public interface IEventSourcedAggregateRoot
    {
        #region Methods

        IReadOnlyList<IEvent> GetUncommittedEvents();

        void CommitEvents(long committedVersion);

        void ReplayEvents(IReadOnlyList<EventStream> eventStreams);

        string GetId();

        string GetTypeName();

        long GetVersion();

        #endregion
    }
}
