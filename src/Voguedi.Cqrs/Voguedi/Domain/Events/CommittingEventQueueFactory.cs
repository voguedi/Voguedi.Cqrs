using Microsoft.Extensions.Logging;

namespace Voguedi.Domain.Events
{
    class CommittingEventQueueFactory : ICommittingEventQueueFactory
    {
        #region Private Fields

        readonly ICommittingEventHandler handler;
        readonly ILoggerFactory loggerFactory;

        #endregion

        #region Ctors
        
        public CommittingEventQueueFactory(ICommittingEventHandler handler, ILoggerFactory loggerFactory)
        {
            this.handler = handler;
            this.loggerFactory = loggerFactory;
        }

        #endregion

        #region ICommittingEventQueueFactory

        public ICommittingEventQueue Create(string aggregateRootId) => new CommittingEventQueue(aggregateRootId, handler, loggerFactory.CreateLogger<CommittingEventQueue>());

        #endregion
    }
}
