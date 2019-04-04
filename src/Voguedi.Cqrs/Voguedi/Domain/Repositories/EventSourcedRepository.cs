using System;
using System.Collections.Concurrent;
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
        readonly ConcurrentDictionary<Type, ConstructorInfo> ctorMapping;

        #endregion

        #region Ctors

        public EventSourcedRepository(IEventStore eventStore)
        {
            this.eventStore = eventStore;
            ctorMapping = new ConcurrentDictionary<Type, ConstructorInfo>();
        }

        #endregion

        #region Private Methods

        TAggregateRoot Build<TAggregateRoot, TIdentity>(TIdentity aggregateRootId)
            where TAggregateRoot : class, IAggregateRoot<TIdentity>
        {
            var ctor = ctorMapping.GetOrAddIfNotNull(typeof(TAggregateRoot), CtorFactory);

            if (ctor != null)
                return ctor.Invoke(new object[] { aggregateRootId }) as TAggregateRoot;

            throw new Exception($"聚合根未提供初始化 Id 的构造方法。 [AggregateRootType = {typeof(TAggregateRoot)}]");
        }

        IAggregateRoot Build(Type aggregateRootType, string aggregateRootId)
        {
            var ctor = ctorMapping.GetOrAddIfNotNull(aggregateRootType, CtorFactory);

            if (ctor != null)
                return ctor.Invoke(new[] { Convert.ChangeType(aggregateRootId, ctor.GetParameters().First().ParameterType) }) as IAggregateRoot;

            throw new Exception($"聚合根未提供初始化 Id 的构造方法。 [AggregateRootType = {aggregateRootType}]");
        }

        ConstructorInfo CtorFactory(Type aggregateRootType)
        {
            var ctors = from ctor in aggregateRootType.GetTypeInfo().GetConstructors()
                        where ctor.GetParameters()?.Length == 1
                        select ctor;
            return ctors.FirstOrDefault();
        }

        #endregion

        #region IRepository

        async Task<TAggregateRoot> IRepository.GetAsync<TAggregateRoot, TIdentity>(TIdentity aggregateRootId)
        {
            if (aggregateRootId == null)
                throw new ArgumentNullException(nameof(aggregateRootId));

            var result = await eventStore.GetAllAsync<TAggregateRoot, TIdentity>(aggregateRootId);

            if (result.Succeeded)
            {
                var eventStream = result.Data;

                if (eventStream != null)
                {
                    var aggregateRoot = Build<TAggregateRoot, TIdentity>(aggregateRootId);
                    aggregateRoot.ReplayEvents(eventStream);
                    return aggregateRoot;
                }

                return null;
            }

            throw result.Exception;
        }

        public async Task<IAggregateRoot> GetAsync(Type aggregateRootType, string aggregateRootId)
        {
            if (aggregateRootType == null)
                throw new ArgumentNullException(nameof(aggregateRootType));

            if (string.IsNullOrWhiteSpace(aggregateRootId))
                throw new ArgumentNullException(nameof(aggregateRootId));

            var result = await eventStore.GetAllAsync(aggregateRootType.FullName, aggregateRootId);

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
