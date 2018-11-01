using System.Threading.Tasks;

namespace Voguedi.Domain.Events
{
    public interface IProcessingDomainEventQueue
    {
        #region Methods

        void Enqueue(ProcessingDomainEvent processingEvent);

        void EnqueueToWaiting(ProcessingDomainEvent processingEvent);

        Task CommitAsync(ProcessingDomainEvent processingEvent);

        Task RejectAsync(ProcessingDomainEvent processingEvent);

        #endregion
    }
}
