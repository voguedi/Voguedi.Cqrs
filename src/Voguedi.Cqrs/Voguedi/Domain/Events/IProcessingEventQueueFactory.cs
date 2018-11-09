namespace Voguedi.Domain.Events
{
    public interface IProcessingEventQueueFactory
    {
        #region Methods

        IProcessingEventQueue Create(string aggregateRootId);

        #endregion
    }
}
