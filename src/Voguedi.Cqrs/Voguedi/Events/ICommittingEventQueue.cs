namespace Voguedi.Events
{
    public interface ICommittingEventQueue
    {
        #region Methods

        void Enqueue(CommittingEvent committingEvent);

        void Clear();

        #endregion
    }
}
