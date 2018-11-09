using Voguedi.Messaging;

namespace Voguedi.Domain.Events
{
    public interface IEventProcessor : IMessageService
    {
        #region Methods

        void Process(ProcessingEvent processingEvent);

        #endregion
    }
}
