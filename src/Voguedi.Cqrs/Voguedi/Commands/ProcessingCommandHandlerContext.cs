using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Voguedi.Domain.AggregateRoots;
using Voguedi.Domain.Caching;
using Voguedi.Domain.Repositories;

namespace Voguedi.Commands
{
    class ProcessingCommandHandlerContext : IProcessingCommandHandlerContext
    {
        #region Private Fields

        readonly IRepository repository;
        readonly IServiceProvider serviceProvider;
        readonly ConcurrentDictionary<string, IEventSourcedAggregateRoot> aggregateRootMapping = new ConcurrentDictionary<string, IEventSourcedAggregateRoot>();

        #endregion

        #region Ctors

        public ProcessingCommandHandlerContext(IRepository repository, IServiceProvider serviceProvider)
        {
            this.repository = repository;
            this.serviceProvider = serviceProvider;
        }

        #endregion

        #region IProcessingCommandHandlerContext

        public IReadOnlyList<IEventSourcedAggregateRoot> GetAggregateRoots() => aggregateRootMapping.Values.ToList();

        Task ICommandHandlerContext.CreateAggregateRootAsync<TAggregateRoot, TIdentity>(TAggregateRoot aggregateRoot)
        {
            if (aggregateRoot == null)
                throw new ArgumentNullException(nameof(aggregateRoot));

            if (Equals(aggregateRoot.Id, default(TIdentity)))
                throw new ArgumentException(nameof(aggregateRoot), $"聚合根 Id 不能为空！");

            if (!aggregateRootMapping.TryAdd(aggregateRoot.Id.ToString(), aggregateRoot))
                throw new ArgumentException(nameof(aggregateRoot), $"聚合根已创建！");

            return Task.CompletedTask;
        }

        async Task<TAggregateRoot> ICommandHandlerContext.GetAggregateRootAsync<TAggregateRoot, TIdentity>(TIdentity aggregateRootId)
        {
            if (Equals(aggregateRootId, default(TIdentity)))
                throw new ArgumentNullException(nameof(aggregateRootId));

            var key = aggregateRootId.ToString();

            if (aggregateRootMapping.TryGetValue(key, out var value) && value is TAggregateRoot aggregateRoot)
                return aggregateRoot;

            aggregateRoot = await serviceProvider.GetService<ICache>()?.GetAsync<TAggregateRoot, TIdentity>(aggregateRootId);

            if (aggregateRoot == null)
                aggregateRoot = await repository.Get<TAggregateRoot, TIdentity>(aggregateRootId);

            aggregateRoot = await repository.Get<TAggregateRoot, TIdentity>(aggregateRootId);

            if (aggregateRoot != null)
            {
                aggregateRootMapping.TryAdd(aggregateRoot.GetId(), aggregateRoot);
                return aggregateRoot;
            }

            return null;
        }

        #endregion
    }
}
