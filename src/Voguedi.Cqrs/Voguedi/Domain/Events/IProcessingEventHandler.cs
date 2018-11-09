using System.Threading.Tasks;

namespace Voguedi.Domain.Events
{
    public interface IProcessingEventHandler
    {
        #region Methods

        Task HandleAsync(ProcessingEvent processingEvent);

        #endregion
    }
}
