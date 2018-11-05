using System.Threading.Tasks;

namespace Voguedi.Commands
{
    public interface ICommandProcessor
    {
        #region Methods

        Task ProcessAsync(ProcessingCommand processingCommand);

        #endregion
    }
}
