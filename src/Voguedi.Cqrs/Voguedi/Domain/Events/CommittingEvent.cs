using System.Threading.Tasks;
using Voguedi.Commands;
using Voguedi.Domain.AggregateRoots;

namespace Voguedi.Domain.Events
{
    public class CommittingEvent
    {
        #region Ctors

        public CommittingEvent(EventStream stream, ProcessingCommand processingCommand, IAggregateRoot aggregateRoot)
        {
            Stream = stream;
            ProcessingCommand = processingCommand;
            AggregateRoot = aggregateRoot;
        }

        #endregion

        #region Public Properties

        public EventStream Stream { get; }

        public ProcessingCommand ProcessingCommand { get; }

        public IAggregateRoot AggregateRoot { get; }

        public ICommittingEventQueue Queue { get; set; }

        #endregion

        #region Public Methods

        public Task OnQueueCommitted() => Queue.CommitAsync();

        #endregion
    }
}
