using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Voguedi.Domain.AggregateRoots;
using Voguedi.Domain.Snapshots;
using Voguedi.Events;

namespace Voguedi.Domain.Repositories
{
    class EventSourcedRepository : IRepository
    {
        #region Private Fields

        readonly IEventStore eventStore;
        readonly IServiceProvider serviceProvider;

        #endregion

        #region Private Methods

        async Task<TAggregateRoot> TryRestoreFromSnapshotAsync<TAggregateRoot, TIdentity>(TIdentity id)
            where TAggregateRoot : class, IAggregateRoot<TIdentity>
        {
            var aggregateRoot = await serviceProvider.GetService<ISnapshot>()?.RestoreAsync<TAggregateRoot, TIdentity>(id);

            if (aggregateRoot != null)
            {
                var result = await eventStore.GetStreamsAsync(typeof(TAggregateRoot).FullName, id.ToString(), aggregateRoot.GetVersion() + 1);

                if (result.Succeeded)
                {
                    aggregateRoot.ReplayEvents(result.Data);
                    return aggregateRoot;
                }

                throw result.Exception;
            }

            return null;
        }

        TAggregateRoot Build<TAggregateRoot, TIdentity>(TIdentity id)
           where TAggregateRoot : class, IAggregateRoot<TIdentity>
        {
            var ctors = from ctor in typeof(TAggregateRoot).GetTypeInfo().GetConstructors()
                        let parameters = ctor.GetParameters()
                        where parameters.Length == 1 && parameters[0].ParameterType == typeof(TIdentity)
                        select ctor;
            var defaultCtor = ctors.FirstOrDefault();

            if (defaultCtor != null)
                return (TAggregateRoot)defaultCtor.Invoke(new object[] { id });

            throw new Exception($"聚合根未提供包含一个参数的构造函数！");
        }

        #endregion

        #region IRepository

        async Task<TAggregateRoot> IRepository.Get<TAggregateRoot, TIdentity>(TIdentity id)
        {
            if (Equals(id, default(TIdentity)))
                throw new ArgumentNullException(nameof(id));

            var aggregateRoot = await TryRestoreFromSnapshotAsync<TAggregateRoot, TIdentity>(id);

            if (aggregateRoot != null)
            {
                aggregateRoot = Build<TAggregateRoot, TIdentity>(id);
                var result = await eventStore.GetStreamsAsync(typeof(TAggregateRoot).FullName, id.ToString());

                if (result.Succeeded)
                {
                    aggregateRoot.ReplayEvents(result.Data);
                    return aggregateRoot;
                }

                throw result.Exception;
            }

            return null;
        }

        #endregion
    }
}
