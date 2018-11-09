using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Voguedi.Messaging;
using Voguedi.ObjectSerialization;
using Voguedi.Reflection;

namespace Voguedi.Domain.Events
{
    class EventSubscriber : MessageSubscriber, IEventSubscriber
    {
        #region Private Fields

        readonly IEventProcessor processor;
        readonly IStringObjectSerializer objectSerializer;

        #endregion

        #region Ctors

        public EventSubscriber(
            IEventProcessor processor,
            IStringObjectSerializer objectSerializer,
            IMessageConsumerFactory consumerFactory,
            ITypeFinder typeFinder,
            ILogger<EventSubscriber> logger,
            VoguediOptions options)
            : base(consumerFactory, typeFinder, logger, options.DefaultEventGroupName, options.DefaultTopicQueueCount)
        {
            this.processor = processor;
            this.objectSerializer = objectSerializer;
        }

        #endregion

        #region MessageSubscriber

        protected override Type GetSubscriberBaseType() => typeof(IEvent);

        protected override void Process(ReceivingMessage receivingMessage, IMessageConsumer consumer)
        {
            var message = objectSerializer.Deserialize<EventStreamMessage>(receivingMessage.QueueMessage);
            var events = new List<IEvent>();

            foreach (var item in message.Events)
                events.Add((IEvent)objectSerializer.Deserialize(item.Value, Type.GetType(item.Key)));

            var stream = new EventStream(
                message.Id,
                message.Timestamp,
                message.CommandId,
                message.AggregateRootTypeName,
                message.AggregateRootId,
                message.Version,
                events);
            processor.Process(new ProcessingEvent(stream, consumer));
        }

        #endregion
    }
}
