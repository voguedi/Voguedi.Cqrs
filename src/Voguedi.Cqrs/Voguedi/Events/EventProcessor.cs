using System;
using System.Collections.Concurrent;

namespace Voguedi.Events
{
    class EventProcessor : IEventProcessor
    {
        #region Private Fields

        readonly IProcessingEventQueueFactory queueFactory;
        readonly ConcurrentDictionary<string, IProcessingEventQueue> queueMapping = new ConcurrentDictionary<string, IProcessingEventQueue>();

        #endregion

        #region Ctors

        public EventProcessor(IProcessingEventQueueFactory queueFactory) => this.queueFactory = queueFactory;

        #endregion

        #region IEventProcessor

        public void Process(ProcessingEvent processingEvent)
        {
            var aggregateRootId = processingEvent.Stream.AggregateRootId;

            if (string.IsNullOrWhiteSpace(aggregateRootId))
                throw new ArgumentException(nameof(processingEvent), $"处理事件的聚合根 Id 不能为空！");

            var queue = queueMapping.GetOrAdd(aggregateRootId, key => queueFactory.Create(key));
            queue.Enqueue(processingEvent);
        }

        #endregion
    }
}
