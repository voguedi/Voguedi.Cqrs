using Voguedi.Messaging;

namespace Voguedi.Commands
{
    public interface ICommandProcessor : IMessageService
    {
        #region Methods

        void Process(ProcessingCommand processingCommand);

        #endregion
    }
}
