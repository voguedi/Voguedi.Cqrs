using Voguedi.Services;

namespace Voguedi.Commands
{
    public interface ICommandProcessor : IService
    {
        #region Methods

        void Process(ProcessingCommand processingCommand);

        #endregion
    }
}
