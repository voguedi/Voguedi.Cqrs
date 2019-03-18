using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Voguedi.Domain.AggregateRoots;
using Voguedi.Domain.Events;

namespace Voguedi.Domain.Repositories
{
    class EventSourcedRepository : IRepository
    {
        #region Private Fields

        readonly IEventStore eventStore;

        #endregion

        #region Ctors
        
        public EventSourcedRepository(IEventStore eventStore) => this.eventStore = eventStore;

        #endregion

        #region Private Methods

        IEventSourcedAggregateRoot Build(Type aggregateRootType, object aggregateRootId)
        {
            var ctors = from ctor in aggregateRootType.GetTypeInfo().GetConstructors()
                        let parameters = ctor.GetParameters()
                        where parameters.Length == 1
                        select ctor;
            var defaultCtor = ctors.FirstOrDefault();

            if (defaultCtor != null)
                return defaultCtor.Invoke(new[] { aggregateRootId }) as IEventSourcedAggregateRoot;

            throw new Exception($"聚合根 {aggregateRootType} 未提供初始化 Id 的构造方法！");
        }

        #endregion

        #region IRepository

        async Task<IEventSourcedAggregateRoot> IRepository.GetAsync(Type aggregateRootType, object aggregateRootId)
        {
            if (aggregateRootType == null)
                throw new ArgumentNullException(nameof(aggregateRootType));

            if (aggregateRootId == null)
                throw new ArgumentNullException(nameof(aggregateRootId));

            var result = await eventStore.GetAllAsync(aggregateRootType.FullName, aggregateRootId.ToString());

            if (result.Succeeded)
            {
                var eventStream = result.Data;

                if (eventStream != null)
                {
                    var aggregateRoot = Build(aggregateRootType, aggregateRootId);
                    aggregateRoot.ReplayEvents(eventStream);
                    return aggregateRoot;
                }

                return null;
            }

            throw result.Exception;
        }

        #endregion
    }
}
