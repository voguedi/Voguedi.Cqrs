using System;
using System.Collections.Generic;
using Voguedi.Domain.Events;

namespace Voguedi.Domain.AggregateRoots
{
    public interface IEventSourcedAggregateRoot
    {
        #region Properties

        long Version { get; }

        #endregion

        #region Methods

        Type GetAggregateRootType();

        string GetAggregateRootId();

        IReadOnlyList<IEvent> GetUncommittedEvents();

        void CommitEvents(long committedVersion);

        void ReplayEvents(IReadOnlyList<EventStream> eventStreams);

        #endregion
    }
}
