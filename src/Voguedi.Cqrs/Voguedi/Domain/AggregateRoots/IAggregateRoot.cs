using System.Collections.Generic;
using Voguedi.Domain.Events;

namespace Voguedi.Domain.AggregateRoots
{
    public interface IAggregateRoot
    {
        #region Properties

        string Id { get; }

        long Version { get; }

        #endregion

        #region Methods

        IReadOnlyList<IEvent> GetUncommittedEvents();

        void CommitEvents(long committedVersion);

        void ReplayEvents(IReadOnlyList<EventStream> eventStreams);

        #endregion
    }

    public interface IAggregateRoot<TIdentity> : IAggregateRoot
    {
        #region Properties

        new TIdentity Id { get; }

        #endregion
    }
}
