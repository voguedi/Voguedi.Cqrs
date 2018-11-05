using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Voguedi.Commands
{
    class CommandProcessor : ICommandProcessor
    {
        #region Private Fields

        readonly IProcessingCommandQueueFactory queueFactory;
        readonly ConcurrentDictionary<string, IProcessingCommandQueue> queueMapping = new ConcurrentDictionary<string, IProcessingCommandQueue>();

        #endregion

        #region Ctors

        public CommandProcessor(IProcessingCommandQueueFactory queueFactory) => this.queueFactory = queueFactory;

        #endregion

        #region ICommandProcessor

        public Task ProcessAsync(ProcessingCommand processingCommand)
        {
            var command = processingCommand.Command;
            var aggregateRootId = command.AggregateRootId;

            if (string.IsNullOrWhiteSpace(aggregateRootId))
                throw new ArgumentException(nameof(processingCommand), $"命令处理的聚合根 Id 不能为空！ [CommandType = {command.GetType()}, CommandId = {command.Id}]");

            var queue = queueMapping.GetOrAdd(aggregateRootId, key => queueFactory.Create(key));
            queue.Enqueue(processingCommand);
            return Task.CompletedTask;
        }

        #endregion
    }
}
