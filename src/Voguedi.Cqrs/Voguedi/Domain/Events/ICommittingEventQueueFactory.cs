namespace Voguedi.Domain.Events
{
    public interface ICommittingEventQueueFactory
    {
        #region Methods

        ICommittingEventQueue Create(string aggregateRootId);

        #endregion
    }
}
