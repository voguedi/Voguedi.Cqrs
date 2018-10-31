using System.Threading.Tasks;
using Voguedi.Messaging;

namespace Voguedi.Commands
{
    public sealed class ProcessingCommand
    {
        #region Public Properties

        public IMessageConsumer Consumer { get; }

        public ICommand Command { get; }

        public IProcessingCommandQueue Queue { get; set; }

        public long QueueSequence { get; set; }

        #endregion

        #region Ctors

        public ProcessingCommand(IMessageConsumer consumer, ICommand command)
        {
            Consumer = consumer;
            Command = command;
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

        #endregion
    }
}
