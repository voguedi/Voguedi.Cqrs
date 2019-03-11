using System.Threading.Tasks;
using Voguedi.Messaging;

namespace Voguedi.ApplicationMessages
{
    public class ProcessingApplicationMessage
    {
        #region Ctors

        public ProcessingApplicationMessage(IApplicationMessage applicationMessage, IMessageConsumer consumer)
        {
            ApplicationMessage = applicationMessage;
            Consumer = consumer;
        }

        #endregion

        #region Public Properties

        public IApplicationMessage ApplicationMessage { get; }

        public IMessageConsumer Consumer { get; }

        public IProcessingApplicationMessageQueue Queue { get; set; }

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

        #endregion
    }
}
