using System.Threading.Tasks;

namespace Voguedi.Domain.Events
{
    public interface IProcessingDomainEventHandler
    {
        #region Methods

        Task HandleAsync(ProcessingDomainEvent processingEvent);

        #endregion
    }
}
