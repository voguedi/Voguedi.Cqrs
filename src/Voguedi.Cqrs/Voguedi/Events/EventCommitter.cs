using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Voguedi.Events
{
    class EventCommitter : IEventCommitter
    {
        #region Private Fields

        readonly ICommittingEventQueueFactory queueFactory;
        readonly ConcurrentDictionary<string, ICommittingEventQueue> queueMapping = new ConcurrentDictionary<string, ICommittingEventQueue>();

        #endregion

        #region Ctors

        public EventCommitter(ICommittingEventQueueFactory queueFactory) => this.queueFactory = queueFactory;

        #endregion

        #region IEventCommitter

        public Task CommitAsync(CommittingEvent committingEvent)
        {
            var aggregateRootId = committingEvent.ProcessingCommand.Command.GetAggregateRootId();

            if (string.IsNullOrWhiteSpace(aggregateRootId))
                throw new ArgumentException(nameof(committingEvent), $"提交事件的聚合根 Id 不能为空！");

            var queue = queueMapping.GetOrAdd(aggregateRootId, key => queueFactory.Create(key));
            queue.Enqueue(committingEvent);
            return Task.CompletedTask;
        }

        #endregion
    }
}
