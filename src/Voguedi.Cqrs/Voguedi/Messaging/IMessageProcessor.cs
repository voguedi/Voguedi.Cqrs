using Voguedi.Services;

namespace Voguedi.Messaging
{
    public interface IMessageProcessor : IBackgroundWorkerService
    {
        #region Methods

        void Process(ReceivingMessage receivingMessage, IMessageConsumer consumer);

        #endregion
    }
}
