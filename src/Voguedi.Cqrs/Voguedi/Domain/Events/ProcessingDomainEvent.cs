using System.Threading.Tasks;
using Voguedi.Messaging;

namespace Voguedi.Domain.Events
{
    public sealed class ProcessingDomainEvent
    {
        #region Public Properties

        public DomainEventStream Stream { get; }

        public IMessageConsumer Consumer { get; }

        public IProcessingDomainEventQueue Queue { get; set; }

        #endregion

        #region Ctors

        public ProcessingDomainEvent(DomainEventStream stream, IMessageConsumer consumer)
        {
            Stream = stream;
            Consumer = consumer;
        }

        #endregion

        #region Public Methods

        public Task OnConsumerCommittedAsync()
        {
            Consumer.Commit();
            return Task.CompletedTask;
        }

        public Task OnConsumerRejectedAsync()
        {
            Consumer.Reject();
            return Task.CompletedTask;
        }

        public Task OnQueueCommittedAsync() => Queue.CommitAsync(this);

        public Task OnQueueRejectedAsync() => Queue.RejectAsync(this);

        public void EnqueueToWaitingQueue() => Queue.EnqueueToWaiting(this);

        #endregion
    }
}
