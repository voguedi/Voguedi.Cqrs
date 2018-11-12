﻿using System;
using System.Collections.Concurrent;
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
        readonly IMessageQueueTopicProvider queueTopicProvider;
        readonly IStringObjectSerializer objectSerializer;
        readonly string defaultGroupName;
        readonly int defaultTopicQueueCount;
        readonly ConcurrentDictionary<Type, string> topicMapping = new ConcurrentDictionary<Type, string>();

        #endregion

        #region Ctors

        public EventPublisher(IMessageProducer producer, IMessageQueueTopicProvider queueTopicProvider, IStringObjectSerializer objectSerializer, VoguediOptions options)
        {
            this.producer = producer;
            this.queueTopicProvider = queueTopicProvider;
            this.objectSerializer = objectSerializer;
            defaultGroupName = options.DefaultEventGroupName;
            defaultTopicQueueCount = options.DefaultTopicQueueCount;
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

        public Task<AsyncExecutedResult> PublishStreamAsync(EventStream stream)
            => producer.ProduceAsync(queueTopicProvider.Get(stream.Events.First(), defaultGroupName, defaultTopicQueueCount), BuildQueueMessage(stream));

        #endregion
    }
}