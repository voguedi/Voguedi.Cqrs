using System.Threading.Tasks;
using Voguedi.Processors;

namespace Voguedi.Commands
{
    public interface ICommandProcessor : IProcessor
    {
        #region Methods

        Task ProcessAsync(ProcessingCommand processingCommand);

        #endregion
    }
}
