using System.Threading.Tasks;
using Voguedi.Processors;

namespace Voguedi.Events
{
    public interface IEventProcessor : IProcessor
    {
        #region Methods

        Task ProcessAsync(ProcessingEvent processingEvent);

        #endregion
    }
}
