using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Voguedi.BackgroundWorkers;
using Voguedi.Infrastructure;
using Voguedi.ObjectSerializers;

namespace Voguedi.Domain.Events
{
    class EventProcessor : IEventProcessor
    {
        #region Private Fields

        readonly IStringObjectSerializer objectSerializer;
        readonly IProcessingEventQueueFactory queueFactory;
        readonly IBackgroundWorker backgroundWorker;
        readonly ILogger logger;
        readonly int expiration;
        readonly string backgroundWorkerKey;
        readonly ConcurrentDictionary<string, IProcessingEventQueue> queueMapping;
        bool started;
        bool stopped;

        #endregion

        #region Ctors

        public EventProcessor(
            IStringObjectSerializer objectSerializer,
            IProcessingEventQueueFactory queueFactory,
            IBackgroundWorker backgroundWorker,
            ILogger<EventProcessor> logger,
            VoguediOptions options)
        {
            this.objectSerializer = objectSerializer;
            this.queueFactory = queueFactory;
            this.backgroundWorker = backgroundWorker;
            this.logger = logger;
            expiration = options.MemoryQueueExpiration;
            backgroundWorkerKey = $"{nameof(EventProcessor)}_{SnowflakeId.Default().NewId()}";
            queueMapping = new ConcurrentDictionary<string, IProcessingEventQueue>();
        }

        #endregion

        #region Private Methods

        void Clear()
        {
            var queue = new List<KeyValuePair<string, IProcessingEventQueue>>();

            foreach (var item in queueMapping)
            {
                if (item.Value.IsInactive(expiration))
                    queue.Add(item);
            }

            foreach (var item in queue)
            {
                if (queueMapping.TryRemove(item.Key))
                    logger.LogDebug($"已过期队列清理成功。 [AggregateRootId = {item.Key}, Expiration = {expiration}]");
            }
        }

        #endregion

        #region IEventProcessor

        public void Process(string receivedMessage)
        {
            var streamMessage = objectSerializer.Deserialize<EventStreamMessage>(receivedMessage);
            var aggregateRootId = streamMessage.AggregateRootId;

            if (string.IsNullOrWhiteSpace(aggregateRootId))
                throw new ArgumentException($"事件处理的聚合根 Id 不能为空。 [EventStreamMessage = {streamMessage}]", nameof(receivedMessage));

            var events = new List<IEvent>();

            foreach (var item in streamMessage.Events)
                events.Add((IEvent)objectSerializer.Deserialize(item.Value, Type.GetType(item.Key)));

            var queue = queueMapping.GetOrAdd(aggregateRootId, queueFactory.Create);
            var stream = new EventStream(
                streamMessage.Id,
                streamMessage.Timestamp,
                streamMessage.CommandId,
                streamMessage.AggregateRootTypeName,
                streamMessage.AggregateRootId,
                streamMessage.Version,
                events);
            queue.Enqueue(new ProcessingEvent(stream));
        }

        public void Start()
        {
            if (!started)
            {
                backgroundWorker.Start(backgroundWorkerKey, Clear, expiration, expiration);
                started = true;
            }
        }

        public void Stop()
        {
            if (!stopped)
            {
                backgroundWorker.Stop(backgroundWorkerKey);
                stopped = true;
            }
        }

        #endregion
    }
}
