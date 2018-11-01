namespace Voguedi.Commands
{
    public interface ICommandProcessor
    {
        #region Methods

        void Process(ProcessingCommand processingCommand);

        #endregion
    }
}
