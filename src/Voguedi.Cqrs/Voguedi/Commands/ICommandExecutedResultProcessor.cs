using System.Threading.Tasks;

namespace Voguedi.Commands
{
    public interface ICommandExecutedResultProcessor
    {
        #region Methods

        Task ProcessAsync(ProcessingCommand processingCommand, CommandExecutedResult result);

        #endregion
    }
}
