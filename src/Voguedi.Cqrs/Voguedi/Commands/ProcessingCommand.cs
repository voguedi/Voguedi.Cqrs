using System.Threading.Tasks;
using Voguedi.Messaging;

namespace Voguedi.Commands
{
    public sealed class ProcessingCommand
    {
        #region Ctors

        public ProcessingCommand(ICommand command, IMessageConsumer consumer)
        {
            Command = command;
            Consumer = consumer;
        }

        #endregion

        #region Public Properties

        public ICommand Command { get; }

        public IMessageConsumer Consumer { get; }

        public IProcessingCommandQueue Queue { get; set; }

        public long QueueSequence { get; set; }

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
