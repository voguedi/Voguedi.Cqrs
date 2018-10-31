using System.Threading.Tasks;

namespace Voguedi.Commands
{
    public interface IProcessingCommandHandler
    {
        #region Methods

        Task HandleAsync(ProcessingCommand processingCommand);

        #endregion
    }
}
