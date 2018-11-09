using System.Threading.Tasks;

namespace Voguedi.Domain.Events
{
    public interface IProcessingEventQueue
    {
        #region Methods

        void Enqueue(ProcessingEvent processingEvent);

        void EnqueueToWaiting(ProcessingEvent processingEvent);

        Task CommitAsync(ProcessingEvent processingEvent);

        Task RejectAsync(ProcessingEvent processingEvent);

        bool IsInactive(int expiration);

        #endregion
    }
}
