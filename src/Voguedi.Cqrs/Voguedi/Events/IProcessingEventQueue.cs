using System.Threading.Tasks;

namespace Voguedi.Events
{
    public interface IProcessingEventQueue
    {
        #region Methods

        void Enqueue(ProcessingEvent processingEvent);

        void EnqueueToWaiting(ProcessingEvent processingEvent);

        Task CommitAsync(ProcessingEvent processingEvent);

        Task RejectAsync(ProcessingEvent processingEvent);

        #endregion
    }
}
