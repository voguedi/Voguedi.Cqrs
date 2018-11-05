using Voguedi.Commands;
using Voguedi.Domain.AggregateRoots;

namespace Voguedi.Events
{
    public sealed class CommittingEvent
    {
        #region Public Properties

        public EventStream Stream { get; }

        public ProcessingCommand ProcessingCommand { get; }

        public IEventSourcedAggregateRoot AggregateRoot { get; }

        public ICommittingEventQueue Queue { get; set; }

        #endregion

        #region Ctors

        public CommittingEvent(EventStream stream, ProcessingCommand processingCommand, IEventSourcedAggregateRoot aggregateRoot)
        {
            Stream = stream;
            ProcessingCommand = processingCommand;
            AggregateRoot = aggregateRoot;
        }

        #endregion
    }
}
