using System.Threading.Tasks;
using Voguedi.Infrastructure;
using Voguedi.MessageQueues;
using Voguedi.ObjectSerializers;

namespace Voguedi.Messaging
{
    class MessagePublisher : IMessagePublisher
    {
        #region Private Fields

        readonly IMessageQueueProducer queueProducer;
        readonly IMessageSubscriptionManager subscriptionManager;
        readonly IStringObjectSerializer objectSerializer;

        #endregion

        #region Ctors

        public MessagePublisher(IMessageQueueProducer queueProducer, IMessageSubscriptionManager subscriptionManager, IStringObjectSerializer objectSerializer)
        {
            this.queueProducer = queueProducer;
            this.subscriptionManager = subscriptionManager;
            this.objectSerializer = objectSerializer;
        }

        #endregion

        #region IMessagePublisher

        public Task<AsyncExecutedResult> PublishAsync(IMessage message)
        {
            return queueProducer.ProduceAsync(subscriptionManager.GetQueueTopic(message), objectSerializer.Serialize(new QueueMessage
            {
                Content = objectSerializer.Serialize(message),
                Tag = message.GetTag()
            }));
        }

        #endregion
    }
}
