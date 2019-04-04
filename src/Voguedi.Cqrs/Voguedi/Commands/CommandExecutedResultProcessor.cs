using System.Threading.Tasks;

namespace Voguedi.Commands
{
    class CommandExecutedResultProcessor : ICommandExecutedResultProcessor
    {
        #region ICommandExecutedResultProcessor

        public Task ProcessAsync(ProcessingCommand processingCommand, CommandExecutedResult result) => Task.CompletedTask;

        #endregion
    }
}
