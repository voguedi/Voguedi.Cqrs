namespace Voguedi.Commands
{
    public interface IProcessingCommandHandlerContextFactory
    {
        #region Methods

        IProcessingCommandHandlerContext Create();

        #endregion
    }
}
