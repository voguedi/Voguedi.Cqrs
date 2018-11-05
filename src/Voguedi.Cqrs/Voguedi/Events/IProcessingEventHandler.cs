using System.Threading.Tasks;

namespace Voguedi.Events
{
    public interface IProcessingEventHandler
    {
        #region Methods

        Task HandleAsync(ProcessingEvent processingEvent);

        #endregion
    }
}
