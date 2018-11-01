using Microsoft.Extensions.Logging;

namespace Voguedi.Domain.Events
{
    class CommittingDomainEventQueueFactory : ICommittingDomainEventQueueFactory
    {
        #region Private Fields

        readonly ICommittingDomainEventHandler handler;
        readonly ILoggerFactory loggerFactory;

        #endregion

        #region Ctors
        
        public CommittingDomainEventQueueFactory(ICommittingDomainEventHandler handler, ILoggerFactory loggerFactory)
        {
            this.handler = handler;
            this.loggerFactory = loggerFactory;
        }

        #endregion

        #region ICommittingDomainEventQueueFactory

        public ICommittingDomainEventQueue Create(string aggregateRootId) => new CommittingDomainEventQueue(aggregateRootId, handler, loggerFactory.CreateLogger<CommittingDomainEventQueue>());

        #endregion
    }
}
