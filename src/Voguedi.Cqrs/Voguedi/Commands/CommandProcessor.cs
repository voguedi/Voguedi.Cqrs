using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Voguedi.BackgroundWorkers;
using Voguedi.DisposableObjects;
using Voguedi.IdentityGeneration;

namespace Voguedi.Commands
{
    class CommandProcessor : DisposableObject, ICommandProcessor
    {
        #region Private Fields

        readonly IProcessingCommandQueueFactory queueFactory;
        readonly IBackgroundWorker backgroundWorker;
        readonly ILogger logger;
        readonly int expiration;
        readonly string backgroundWorkerKey;
        readonly ConcurrentDictionary<string, IProcessingCommandQueue> queueMapping = new ConcurrentDictionary<string, IProcessingCommandQueue>();
        bool disposed;
        bool started;

        #endregion

        #region Ctors

        public CommandProcessor(
            IProcessingCommandQueueFactory queueFactory,
            IBackgroundWorker backgroundWorker,
            IStringIdentityGenerator identityGenerator,
            ILogger<CommandProcessor> logger,
            VoguediOptions options)
        {
            this.queueFactory = queueFactory;
            this.backgroundWorker = backgroundWorker;
            this.logger = logger;
            expiration = options.MemoryQueueExpiration;
            backgroundWorkerKey = $"{nameof(CommandProcessor)}_{identityGenerator.Generate()}";
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
                    logger.LogInformation($"命令处理队列清理成功！ [AggregateRootId = {item.Key}, QueueActiveExpiration = {expiration}]");
            }
        }

        #endregion

        #region DisposableObject

        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                    backgroundWorker.Stop(backgroundWorkerKey);

                disposed = true;
            }
        }

        #endregion

        #region ICommandProcessor

        public void Process(ProcessingCommand processingCommand)
        {
            var command = processingCommand.Command;
            var aggregateRootId = command.AggregateRootId;

            if (string.IsNullOrWhiteSpace(aggregateRootId))
                throw new ArgumentException(nameof(processingCommand), $"命令处理的聚合根 Id 不能为空！ [CommandType = {command.GetType()}, CommandId = {command.Id}]");

            var queue = queueMapping.GetOrAdd(aggregateRootId, queueFactory.Create);
            queue.Enqueue(processingCommand);
        }

        public void Start()
        {
            if (!started)
            {
                backgroundWorker.Start(backgroundWorkerKey, Clear, expiration, expiration);
                started = true;
            }
        }

        #endregion
    }
}
