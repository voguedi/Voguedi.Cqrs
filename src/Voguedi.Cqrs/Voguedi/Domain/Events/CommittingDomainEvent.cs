using Voguedi.Commands;
using Voguedi.Domain.AggregateRoots;

namespace Voguedi.Domain.Events
{
    public sealed class CommittingDomainEvent
    {
        #region Public Properties

        public ProcessingCommand ProcessingCommand { get; }

        public IEventSourcedAggregateRoot AggregateRoot { get; }

        public DomainEventStream Stream { get; }

        #endregion

        #region Ctors

        public CommittingDomainEvent(ProcessingCommand processingCommand, IEventSourcedAggregateRoot aggregateRoot, DomainEventStream stream)
        {
            ProcessingCommand = processingCommand;
            AggregateRoot = aggregateRoot;
            Stream = stream;
        }

        #endregion
    }
}
