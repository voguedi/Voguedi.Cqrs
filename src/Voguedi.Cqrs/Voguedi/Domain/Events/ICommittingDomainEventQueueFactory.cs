namespace Voguedi.Domain.Events
{
    public interface ICommittingDomainEventQueueFactory
    {
        #region Methods

        ICommittingDomainEventQueue Create(string aggregateRootId);

        #endregion
    }
}
