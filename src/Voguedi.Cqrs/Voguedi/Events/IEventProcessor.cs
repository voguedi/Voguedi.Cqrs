using Voguedi.Messaging;

namespace Voguedi.Events
{
    public interface IEventProcessor : IMessageService
    {
        #region Methods

        void Process(ProcessingEvent processingEvent);

        #endregion
    }
}
