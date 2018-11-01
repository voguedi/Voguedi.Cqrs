namespace Voguedi.Domain.Events
{
    public interface ICommittingDomainEventQueue
    {
        #region Methods

        void Enqueue(CommittingDomainEvent committingEvent);

        void Clear();

        void Stop();

        #endregion
    }
}
