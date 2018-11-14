using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Voguedi.AsyncExecution;
using Voguedi.Messaging;
using Voguedi.ObjectSerialization;

namespace Voguedi.Domain.Events
{
    class EventPublisher : IEventPublisher
    {
        #region Private Fields

        readonly IMessageProducer producer;
        readonly IMessageSubscriptionManager subscriptionManager;
        readonly IStringObjectSerializer objectSerializer;

        #endregion

        #region Ctors

        public EventPublisher(IMessageProducer producer, IMessageSubscriptionManager subscriptionManager, IStringObjectSerializer objectSerializer)
        {
            this.producer = producer;
            this.subscriptionManager = subscriptionManager;
            this.objectSerializer = objectSerializer;
        }

        #endregion

        #region Private Methods

        string BuildQueueMessage(EventStream stream)
        {
            var events = new Dictionary<string, string>();

            foreach (var e in stream.Events)
                events.Add(e.GetType().AssemblyQualifiedName, objectSerializer.Serialize(e));

            var streamMessage = new EventStreamMessage
            {
                AggregateRootId = stream.AggregateRootId,
                AggregateRootTypeName = stream.AggregateRootTypeName,
                CommandId = stream.CommandId,
                Events = events,
                Id = stream.Id,
                Timestamp = stream.Timestamp,
                Version = stream.Version
            };
            return objectSerializer.Serialize(streamMessage);
        }

        #endregion

        #region IEventPublisher

        public Task<AsyncExecutedResult> PublishStreamAsync(EventStream stream) => producer.ProduceAsync(subscriptionManager.GetQueueTopic(stream.Events.First()), BuildQueueMessage(stream));

        #endregion
    }
}
