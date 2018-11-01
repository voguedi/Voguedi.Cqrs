namespace Voguedi.Commands
{
    public interface IProcessingCommandQueueFactory
    {
        #region Methods

        IProcessingCommandQueue Create(string aggregateRootId);

        #endregion
    }
}
