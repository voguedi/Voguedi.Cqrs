using Microsoft.Extensions.Logging;

namespace Voguedi.Commands
{
    class ProcessingCommandQueueFactory : IProcessingCommandQueueFactory
    {
        #region Private Fields

        readonly IProcessingCommandHandler handler;
        readonly ICommandExecutedResultProcessor executedResultProcessor;
        readonly ILoggerFactory loggerFactory;

        #endregion

        #region Ctors

        public ProcessingCommandQueueFactory(IProcessingCommandHandler handler, ICommandExecutedResultProcessor executedResultProcessor, ILoggerFactory loggerFactory)
        {
            this.handler = handler;
            this.executedResultProcessor = executedResultProcessor;
            this.loggerFactory = loggerFactory;
        }

        #endregion

        #region IProcessingCommandQueueFactory

        public IProcessingCommandQueue Create(string aggregateRootId)
            => new ProcessingCommandQueue(aggregateRootId, handler, executedResultProcessor, loggerFactory.CreateLogger<ProcessingCommandQueue>());

        #endregion
    }
}
