namespace Voguedi.Domain.Events
{
    public interface IDomainEventCommitter
    {
        #region Methods

        void Commit(CommittingDomainEvent committingEvent);

        #endregion
    }
}
