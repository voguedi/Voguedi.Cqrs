using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Voguedi.BackgroundWorkers;
using Voguedi.Messaging;
using Voguedi.ObjectSerializing;
using Voguedi.Utils;

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
        readonly ConcurrentDictionary<string, IProcessingEventQueue> queueMapping = new ConcurrentDictionary<string, IProcessingEventQueue>();
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
            backgroundWorkerKey = $"{nameof(EventProcessor)}_{SnowflakeId.Instance.NewId()}";
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
                    logger.LogInformation($"不活跃命令处理队列清理成功！ [AggregateRootId = {item.Key}, Expiration = {expiration}]");
            }
        }

        #endregion

        #region IEventProcessor

        public void Process(ReceivingMessage receivingMessage, IMessageConsumer consumer)
        {
            var streamMessage = objectSerializer.Deserialize<EventStreamMessage>(receivingMessage.QueueMessage);
            var aggregateRootId = streamMessage.AggregateRootId;

            if (string.IsNullOrWhiteSpace(aggregateRootId))
                throw new Exception($"事件处理的聚合根 Id 不能为空！ [EventStreamMessage = {streamMessage}]");

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
            queue.Enqueue(new ProcessingEvent(stream, consumer));
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
