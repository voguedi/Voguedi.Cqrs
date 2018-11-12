using Voguedi.Services;

namespace Voguedi.Domain.Events
{
    public interface IEventProcessor : IService
    {
        #region Methods

        void Process(ProcessingEvent processingEvent);

        #endregion
    }
}
