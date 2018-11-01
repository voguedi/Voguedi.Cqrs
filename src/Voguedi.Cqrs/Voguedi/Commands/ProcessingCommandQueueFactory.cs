using Microsoft.Extensions.Logging;

namespace Voguedi.Commands
{
    class ProcessingCommandQueueFactory : IProcessingCommandQueueFactory
    {
        #region Private Fields

        readonly IProcessingCommandHandler handler;
        readonly ILoggerFactory loggerFactory;

        #endregion

        #region Ctors

        public ProcessingCommandQueueFactory(IProcessingCommandHandler handler, ILoggerFactory loggerFactory)
        {
            this.handler = handler;
            this.loggerFactory = loggerFactory;
        }

        #endregion

        #region IProcessingCommandQueueFactory

        public IProcessingCommandQueue Create(string aggregateRootId) => new ProcessingCommandQueue(aggregateRootId, handler, loggerFactory.CreateLogger<ProcessingCommandQueue>());

        #endregion
    }
}
