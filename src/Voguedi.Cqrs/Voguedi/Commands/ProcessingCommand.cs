using System.Threading.Tasks;

namespace Voguedi.Commands
{
    public class ProcessingCommand
    {
        #region Ctors

        public ProcessingCommand(ICommand command) => Command = command;

        #endregion

        #region Public Properties

        public ICommand Command { get; }

        public IProcessingCommandQueue Queue { get; set; }

        public long QueueSequence { get; set; }

        #endregion

        #region Public Methods

        public Task OnQueueProcessedAsync(CommandExecutedStatus executedStatus, string message = null, string messageType = null)
            => Queue.ProcessAsync(this, new CommandExecutedResult(executedStatus, Command.Id, Command.AggregateRootId, message, messageType));

        #endregion
    }
}
