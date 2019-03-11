using System.Threading.Tasks;
using Voguedi.AsyncExecution;
using Voguedi.ObjectSerializing;

namespace Voguedi.Messaging
{
    class MessagePublisher : IMessagePublisher
    {
        #region Private Fields

        readonly IMessageProducer producer;
        readonly IMessageSubscriptionManager subscriptionManager;
        readonly IStringObjectSerializer objectSerializer;

        #endregion

        #region Ctors

        public MessagePublisher(IMessageProducer producer, IMessageSubscriptionManager subscriptionManager, IStringObjectSerializer objectSerializer)
        {
            this.producer = producer;
            this.subscriptionManager = subscriptionManager;
            this.objectSerializer = objectSerializer;
        }

        #endregion

        #region Private Methods

        string BuildQueueMessage(IMessage message)
        {
            var queueMessage = new QueueMessage
            {
                Content = objectSerializer.Serialize(message),
                Tag = message.GetType().AssemblyQualifiedName
            };
            return objectSerializer.Serialize(queueMessage);
        }

        #endregion

        #region IMessagePublisher

        public Task<AsyncExecutedResult> PublishAsync(IMessage message) => producer.ProduceAsync(subscriptionManager.GetQueueTopic(message), BuildQueueMessage(message));

        #endregion
    }
}
