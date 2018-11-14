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
        readonly ConcurrentDictionary<string, IEventSourcedAggregateRoot> aggregateRootMapping = new ConcurrentDictionary<string, IEventSourcedAggregateRoot>();

        #endregion

        #region Ctors

        public ProcessingCommandHandlerContext(ICache cache, IRepository repository)
        {
            this.cache = cache;
            this.repository = repository;
        }

        #endregion

        #region IProcessingCommandHandlerContext

        public IReadOnlyList<IEventSourcedAggregateRoot> GetAggregateRoots() => aggregateRootMapping.Values.ToList();

        Task ICommandHandlerContext.CreateAggregateRootAsync<TAggregateRoot, TIdentity>(TAggregateRoot aggregateRoot)
        {
            if (aggregateRoot == null)
                throw new ArgumentNullException(nameof(aggregateRoot));

            if (Equals(aggregateRoot.Id, default(TIdentity)))
                throw new ArgumentException(nameof(aggregateRoot), $"聚合根 Id 不能为空！ [AggregateRootType = {typeof(TAggregateRoot)}, AggregateRootId = {aggregateRoot.Id}]");

            if (!aggregateRootMapping.TryAdd(aggregateRoot.Id.ToString(), aggregateRoot))
                throw new ArgumentException(nameof(aggregateRoot), $"聚合根重复创建！ [AggregateRootType = {typeof(TAggregateRoot)}, AggregateRootId = {aggregateRoot.Id}]");

            return Task.CompletedTask;
        }

        async Task<TAggregateRoot> ICommandHandlerContext.GetAggregateRootAsync<TAggregateRoot, TIdentity>(TIdentity aggregateRootId)
        {
            if (Equals(aggregateRootId, default(TIdentity)))
                throw new ArgumentNullException(nameof(aggregateRootId));

            var key = aggregateRootId.ToString();

            if (aggregateRootMapping.TryGetValue(key, out var value) && value is TAggregateRoot aggregateRoot)
                return aggregateRoot;

            aggregateRoot = await cache.GetAsync(typeof(TAggregateRoot), key) as TAggregateRoot;

            if (aggregateRoot == null)
                aggregateRoot = await repository.GetAsync(typeof(TAggregateRoot), key) as TAggregateRoot;

            if (aggregateRoot != null)
            {
                aggregateRootMapping.TryAdd(aggregateRoot.GetAggregateRootId(), aggregateRoot);
                return aggregateRoot;
            }

            return null;
        }

        #endregion
    }
}
