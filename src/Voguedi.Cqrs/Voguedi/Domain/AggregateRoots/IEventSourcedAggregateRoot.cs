using System;
using System.Collections.Generic;
using Voguedi.Events;

namespace Voguedi.Domain.AggregateRoots
{
    public interface IEventSourcedAggregateRoot
    {
        #region Methods

        Type GetAggregateRootType();

        string GetAggregateRootId();

        long GetVersion();

        IReadOnlyList<IEvent> GetUncommittedEvents();

        void CommitEvents(long committedVersion);

        void ReplayEvents(IReadOnlyList<EventStream> eventStreams);

        #endregion
    }
}
