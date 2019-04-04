using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Voguedi.Infrastructure;
using Voguedi.MessageQueues;
using Voguedi.Messaging;
using Voguedi.ObjectSerializers;

namespace Voguedi.Domain.Events
{
    class EventPublisher : IEventPublisher
    {
        #region Private Fields

        readonly IMessageQueueProducer queueProducer;
        readonly IMessageSubscriptionManager subscriptionManager;
        readonly IStringObjectSerializer objectSerializer;

        #endregion

        #region Ctors

        public EventPublisher(IMessageQueueProducer queueProducer, IMessageSubscriptionManager subscriptionManager, IStringObjectSerializer objectSerializer)
        {
            this.queueProducer = queueProducer;
            this.subscriptionManager = subscriptionManager;
            this.objectSerializer = objectSerializer;
        }

        #endregion

        #region IEventPublisher

        public Task<AsyncExecutedResult> PublishStreamAsync(EventStream stream)
        {
            var events = new Dictionary<string, string>();

            foreach (var e in stream.Events)
                events.Add(e.GetTag(), objectSerializer.Serialize(e));

            return queueProducer.ProduceAsync(
                subscriptionManager.GetQueueTopic(stream.Events.First()),
                objectSerializer.Serialize(new EventStreamMessage
                {
                    AggregateRootId = stream.AggregateRootId,
                    AggregateRootTypeName = stream.AggregateRootTypeName,
                    CommandId = stream.CommandId,
                    Events = events,
                    Id = stream.Id,
                    Timestamp = stream.Timestamp,
                    Version = stream.Version
                }));
        }

        #endregion
    }
}
