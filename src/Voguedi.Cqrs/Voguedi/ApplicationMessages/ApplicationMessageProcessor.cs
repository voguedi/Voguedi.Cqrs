using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Voguedi.BackgroundWorkers;
using Voguedi.Infrastructure;
using Voguedi.Messaging;
using Voguedi.ObjectSerializers;

namespace Voguedi.ApplicationMessages
{
    class ApplicationMessageProcessor : IApplicationMessageProcessor
    {
        #region Private Fields

        readonly IStringObjectSerializer objectSerializer;
        readonly IProcessingApplicationMessageQueueFactory queueFactory;
        readonly IBackgroundWorker backgroundWorker;
        readonly ILogger logger;
        readonly int expiration;
        readonly string backgroundWorkerKey;
        readonly ConcurrentDictionary<string, IProcessingApplicationMessageQueue> queueMapping;
        bool started;
        bool stopped;

        #endregion

        #region Ctors

        public ApplicationMessageProcessor(
            IStringObjectSerializer objectSerializer,
            IProcessingApplicationMessageQueueFactory queueFactory,
            IBackgroundWorker backgroundWorker,
            ILogger<ApplicationMessageProcessor> logger,
            VoguediOptions options)
        {
            this.objectSerializer = objectSerializer;
            this.queueFactory = queueFactory;
            this.backgroundWorker = backgroundWorker;
            this.logger = logger;
            expiration = options.MemoryQueueExpiration;
            backgroundWorkerKey = $"{nameof(ApplicationMessageProcessor)}_{SnowflakeId.Default().NewId()}";
            queueMapping = new ConcurrentDictionary<string, IProcessingApplicationMessageQueue>();
        }

        #endregion

        #region Private Methods

        void Clear()
        {
            var queue = new List<KeyValuePair<string, IProcessingApplicationMessageQueue>>();

            foreach (var item in queueMapping)
            {
                if (item.Value.IsInactive(expiration))
                    queue.Add(item);
            }

            foreach (var item in queue)
            {
                if (queueMapping.TryRemove(item.Key))
                    logger.LogDebug($"已过期队列清理成功。 [RoutingKey = {item.Key}, Expiration = {expiration}]");
            }
        }

        #endregion

        #region IApplicationMessageProcessor

        public void Process(string receivedMessage)
        {
            var queueMessage = objectSerializer.Deserialize<QueueMessage>(receivedMessage);
            var applicationMessage = (IApplicationMessage)objectSerializer.Deserialize(queueMessage.Content, Type.GetType(queueMessage.Tag));
            var routingKey = applicationMessage.GetRoutingKey();

            if (string.IsNullOrWhiteSpace(routingKey))
                throw new ArgumentException($"应用消息的路由键不能为空。 [ApplicationMessageType = {applicationMessage.GetType()}, ApplicationMessageId = {applicationMessage.Id}]", nameof(receivedMessage));

            var queue = queueMapping.GetOrAdd(routingKey, queueFactory.Create);
            queue.Enqueue(new ProcessingApplicationMessage(applicationMessage));
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
