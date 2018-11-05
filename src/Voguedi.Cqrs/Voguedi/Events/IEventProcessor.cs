using System.Threading.Tasks;

namespace Voguedi.Events
{
    public interface IEventProcessor
    {
        #region Methods

        Task ProcessAsync(ProcessingEvent processingEvent);

        #endregion
    }
}
