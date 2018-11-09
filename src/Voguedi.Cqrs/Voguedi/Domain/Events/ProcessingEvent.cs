using System.Threading.Tasks;
using Voguedi.Messaging;

namespace Voguedi.Domain.Events
{
    public sealed class ProcessingEvent
    {
        #region Public Properties

        public EventStream Stream { get; }

        public IMessageConsumer Consumer { get; }

        public IProcessingEventQueue Queue { get; set; }

        #endregion

        #region Ctors

        public ProcessingEvent(EventStream stream, IMessageConsumer consumer)
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
