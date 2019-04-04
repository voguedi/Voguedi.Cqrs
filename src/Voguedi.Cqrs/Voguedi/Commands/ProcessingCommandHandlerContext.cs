using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Voguedi.Domain.AggregateRoots;
using Voguedi.Domain.Caching;
using Voguedi.Domain.Repositories;

namespace Voguedi.Commands
{
    class ProcessingCommandHandlerContext : IProcessingCommandHandlerContext
    {
        #region Private Fields

        readonly ICache cache;
        readonly IRepository repository;
        readonly ConcurrentDictionary<string, IAggregateRoot> aggregateRootMapping;

        #endregion

        #region Ctors

        public ProcessingCommandHandlerContext(ICache cache, IRepository repository)
        {
            this.cache = cache;
            this.repository = repository;
            aggregateRootMapping = new ConcurrentDictionary<string, IAggregateRoot>();
        }

        #endregion

        #region IProcessingCommandHandlerContext

        public IReadOnlyList<IAggregateRoot> GetAggregateRoots() => aggregateRootMapping.Values.ToList();

        Task ICommandHandlerContext.AddAsync<TAggregateRoot, TIdentity>(TAggregateRoot aggregateRoot)
        {
            if (aggregateRoot == null)
                throw new ArgumentNullException(nameof(aggregateRoot));

            if (aggregateRoot.Id == null)
                throw new ArgumentException($"聚合根 Id 不能为空。 [AggregateRootType = {typeof(TAggregateRoot)}]", nameof(aggregateRoot));

            if (aggregateRootMapping.TryAdd(aggregateRoot.Id.ToString(), aggregateRoot))
                return Task.CompletedTask;

            throw new ArgumentException($"聚合根重复创建。 [AggregateRootType = {typeof(TAggregateRoot)}, AggregateRootId = {aggregateRoot.Id}]", nameof(aggregateRoot));
        }

        async Task<TAggregateRoot> ICommandHandlerContext.GetAsync<TAggregateRoot, TIdentity>(TIdentity aggregateRootId)
        {
            if (aggregateRootId == null)
                throw new ArgumentNullException(nameof(aggregateRootId));

            if (aggregateRootMapping.TryGetValue(aggregateRootId.ToString(), out var value) && value is TAggregateRoot aggregateRoot)
                return aggregateRoot;

            aggregateRoot = await cache.GetAsync<TAggregateRoot, TIdentity>(aggregateRootId);

            if (aggregateRoot == null)
                aggregateRoot = await repository.GetAsync<TAggregateRoot, TIdentity>(aggregateRootId);

            if (aggregateRoot != null)
            {
                aggregateRootMapping.TryAdd(aggregateRoot.Id.ToString(), aggregateRoot);
                return aggregateRoot;
            }

            return null;
        }

        #endregion
    }
}
