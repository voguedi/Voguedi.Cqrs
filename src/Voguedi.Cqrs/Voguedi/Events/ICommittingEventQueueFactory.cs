namespace Voguedi.Events
{
    public interface ICommittingEventQueueFactory
    {
        #region Methods

        ICommittingEventQueue Create(string aggregateRootId);

        #endregion
    }
}
