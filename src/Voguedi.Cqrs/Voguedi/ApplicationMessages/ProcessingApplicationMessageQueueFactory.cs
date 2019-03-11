using Microsoft.Extensions.Logging;

namespace Voguedi.ApplicationMessages
{
    class ProcessingApplicationMessageQueueFactory : IProcessingApplicationMessageQueueFactory
    {
        #region Private Fields

        readonly IProcessingApplicationMessageHandler handler;
        readonly ILoggerFactory loggerFactory;

        #endregion

        #region Ctors

        public ProcessingApplicationMessageQueueFactory(IProcessingApplicationMessageHandler handler, ILoggerFactory loggerFactory)
        {
            this.handler = handler;
            this.loggerFactory = loggerFactory;
        }

        #endregion

        #region IProcessingMessageQueueFactory

        public IProcessingApplicationMessageQueue Create(string routingKey)
            => new ProcessingApplicationMessageQueue(routingKey, handler, loggerFactory.CreateLogger<ProcessingApplicationMessageQueue>());

        #endregion
    }
}
