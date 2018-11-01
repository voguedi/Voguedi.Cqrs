using System;
using System.Collections.Concurrent;

namespace Voguedi.Domain.Events
{
    class DomainEventCommitter : IDomainEventCommitter
    {
        #region Private Fields

        readonly ICommittingDomainEventQueueFactory queueFactory;
        readonly ConcurrentDictionary<string, ICommittingDomainEventQueue> queueMapping = new ConcurrentDictionary<string, ICommittingDomainEventQueue>();

        #endregion

        #region Ctors

        public DomainEventCommitter(ICommittingDomainEventQueueFactory queueFactory) => this.queueFactory = queueFactory;

        #endregion

        #region IDomainEventCommitter

        public void Commit(CommittingDomainEvent committingEvent)
        {
            var aggregateRootId = committingEvent.ProcessingCommand.Command.GetAggregateRootId();

            if (string.IsNullOrWhiteSpace(aggregateRootId))
                throw new ArgumentException(nameof(committingEvent), $"提交领域事件的聚合根 Id 不能为空！");

            var queue = queueMapping.GetOrAdd(aggregateRootId, key => queueFactory.Create(key));
            queue.Enqueue(committingEvent);
        }

        #endregion
    }
}
