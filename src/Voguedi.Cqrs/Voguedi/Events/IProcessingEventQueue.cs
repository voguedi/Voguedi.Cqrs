using System.Threading.Tasks;
using Voguedi.ActiveCheckers;

namespace Voguedi.Events
{
    public interface IProcessingEventQueue : IMemoryQueueActiveContext
    {
        #region Methods

        void Enqueue(ProcessingEvent processingEvent);

        void EnqueueToWaiting(ProcessingEvent processingEvent);

        Task CommitAsync(ProcessingEvent processingEvent);

        Task RejectAsync(ProcessingEvent processingEvent);

        #endregion
    }
}
