using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Voguedi.Schedulers;
using Voguedi.DisposableObjects;

namespace Voguedi.Commands
{
    class CommandProcessor : DisposableObject, ICommandProcessor
    {
        #region Private Fields

        readonly IProcessingCommandQueueFactory queueFactory;
        readonly IScheduler scheduler;
        readonly ILogger logger;
        readonly int queueActiveExpiration;
        readonly ConcurrentDictionary<string, IProcessingCommandQueue> queueMapping = new ConcurrentDictionary<string, IProcessingCommandQueue>();
        bool disposed;
        bool started;

        #endregion

        #region Ctors

        public CommandProcessor(IProcessingCommandQueueFactory queueFactory, IScheduler scheduler, ILogger<CommandProcessor> logger, VoguediOptions options)
        {
            this.queueFactory = queueFactory;
            this.scheduler = scheduler;
            this.logger = logger;
            queueActiveExpiration = options.MemoryQueueActiveExpiration;
        }

        #endregion

        #region DisposableObject

        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                    scheduler.Stop(nameof(CommandProcessor));

                disposed = true;
            }
        }

        #endregion

        #region Private Methods

        void ClearInactiveQueue()
        {
            var queue = new List<KeyValuePair<string, IProcessingCommandQueue>>();

            foreach (var item in queueMapping)
            {
                if (item.Value.IsInactive(queueActiveExpiration))
                    queue.Add(item);
            }

            foreach (var item in queue)
            {
                if (queueMapping.TryRemove(item.Key))
                    logger.LogInformation($"不活跃命令处理队列清理成功！ [AggregateRootId = {item.Key}, QueueActiveExpiration = {queueActiveExpiration}]");
            }
        }

        #endregion

        #region ICommandProcessor

        public Task ProcessAsync(ProcessingCommand processingCommand)
        {
            var command = processingCommand.Command;
            var aggregateRootId = command.AggregateRootId;

            if (string.IsNullOrWhiteSpace(aggregateRootId))
                throw new ArgumentException(nameof(processingCommand), $"命令处理的聚合根 Id 不能为空！ [CommandType = {command.GetType()}, CommandId = {command.Id}]");

            var queue = queueMapping.GetOrAdd(aggregateRootId, queueFactory.Create);
            queue.Enqueue(processingCommand);
            return Task.CompletedTask;
        }

        public void Start()
        {
            if (!started)
            {
                scheduler.Start(nameof(CommandProcessor), ClearInactiveQueue, queueActiveExpiration, queueActiveExpiration);
                started = true;
            }
        }

        #endregion
    }
}
