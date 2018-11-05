using Voguedi.ActiveCheckers;

namespace Voguedi.Events
{
    public interface ICommittingEventQueue : IMemoryQueueActiveContext
    {
        #region Methods

        void Enqueue(CommittingEvent committingEvent);

        void Clear();

        #endregion
    }
}
