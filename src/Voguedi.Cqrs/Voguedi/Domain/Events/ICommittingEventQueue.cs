using System.Threading.Tasks;

namespace Voguedi.Domain.Events
{
    public interface ICommittingEventQueue
    {
        #region Methods

        void Enqueue(CommittingEvent committingEvent);

        Task CommitAsync();

        void Clear();

        bool IsInactive(int expiration);

        #endregion
    }
}
