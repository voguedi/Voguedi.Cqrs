using Voguedi.Commands;
using Voguedi.Domain.AggregateRoots;

namespace Voguedi.Domain.Events
{
    public sealed class CommittingDomainEvent
    {
        #region Public Properties

        public DomainEventStream Stream { get; }

        public ProcessingCommand ProcessingCommand { get; }

        public IEventSourcedAggregateRoot AggregateRoot { get; }

        public ICommittingDomainEventQueue Queue { get; set; }

        #endregion

        #region Ctors

        public CommittingDomainEvent(DomainEventStream stream, ProcessingCommand processingCommand, IEventSourcedAggregateRoot aggregateRoot)
        {
            Stream = stream;
            ProcessingCommand = processingCommand;
            AggregateRoot = aggregateRoot;
        }

        #endregion
    }
}
