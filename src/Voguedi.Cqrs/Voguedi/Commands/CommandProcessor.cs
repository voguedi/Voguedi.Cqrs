using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Voguedi.BackgroundWorkers;
using Voguedi.Messaging;
using Voguedi.ObjectSerializing;
using Voguedi.Utils;

namespace Voguedi.Commands
{
    class CommandProcessor : ICommandProcessor
    {
        #region Private Fields
        
        readonly IStringObjectSerializer objectSerializer;
        readonly IProcessingCommandQueueFactory queueFactory;
        readonly IBackgroundWorker backgroundWorker;
        readonly ILogger logger;
        readonly int expiration;
        readonly string backgroundWorkerKey;
        readonly ConcurrentDictionary<string, IProcessingCommandQueue> queueMapping = new ConcurrentDictionary<string, IProcessingCommandQueue>();
        bool started;
        bool stopped;

        #endregion

        #region Ctors

        public CommandProcessor(
            IStringObjectSerializer objectSerializer,
            IProcessingCommandQueueFactory queueFactory,
            IBackgroundWorker backgroundWorker,
            ILogger<CommandProcessor> logger,
            VoguediOptions options)
        {
            this.objectSerializer = objectSerializer;
            this.queueFactory = queueFactory;
            this.backgroundWorker = backgroundWorker;
            this.logger = logger;
            expiration = options.MemoryQueueExpiration;
            backgroundWorkerKey = $"{nameof(CommandProcessor)}_{SnowflakeId.Instance.NewId()}";
        }

        #endregion

        #region Private Methods

        void Clear()
        {
            var queue = new List<KeyValuePair<string, IProcessingCommandQueue>>();

            foreach (var item in queueMapping)
            {
                if (item.Value.IsInactive(expiration))
                    queue.Add(item);
            }

            foreach (var item in queue)
            {
                if (queueMapping.TryRemove(item.Key))
                    logger.LogInformation($"命令处理队列清理成功！ [AggregateRootId = {item.Key}, Expiration = {expiration}]");
            }
        }

        #endregion

        #region ICommandProcessor

        public void Process(ReceivingMessage receivingMessage, IMessageConsumer consumer)
        {
            var queueMessage = objectSerializer.Deserialize<QueueMessage>(receivingMessage.QueueMessage);
            var command = (ICommand)objectSerializer.Deserialize(queueMessage.Content, Type.GetType(queueMessage.Tag));
            var aggregateRootId = command.AggregateRootId;

            if (string.IsNullOrWhiteSpace(aggregateRootId))
                throw new Exception($"命令处理的聚合根 Id 不能为空！ [CommandType = {command.GetType()}, CommandId = {command.Id}]");

            var queue = queueMapping.GetOrAdd(aggregateRootId, queueFactory.Create);
            queue.Enqueue(new ProcessingCommand(command, consumer));
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
