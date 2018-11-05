using Microsoft.Extensions.Logging;

namespace Voguedi.Events
{
    class ProcessingEventQueueFactory : IProcessingEventQueueFactory
    {
        #region Private Fields

        readonly IProcessingEventHandler handler;
        readonly ILoggerFactory loggerFactory;

        #endregion

        #region Ctors

        public ProcessingEventQueueFactory(IProcessingEventHandler handler, ILoggerFactory loggerFactory)
        {
            this.handler = handler;
            this.loggerFactory = loggerFactory;
        }

        #endregion

        #region IProcessingEventQueueFactory

        public IProcessingEventQueue Create(string aggregateRootId) => new ProcessingEventQueue(aggregateRootId, handler, loggerFactory.CreateLogger<ProcessingEventQueue>());

        #endregion
    }
}
