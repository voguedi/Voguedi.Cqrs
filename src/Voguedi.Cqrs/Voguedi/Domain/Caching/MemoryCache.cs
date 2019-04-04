using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Voguedi.BackgroundWorkers;
using Voguedi.Domain.AggregateRoots;
using Voguedi.Domain.Repositories;
using Voguedi.Infrastructure;

namespace Voguedi.Domain.Caching
{
    class MemoryCache : DisposableObject, ICache
    {
        #region Private Class

        class AggregateRootCacheItem
        {
            #region Ctors

            public AggregateRootCacheItem(IAggregateRoot aggregateRoot, DateTime timestamp)
            {
                AggregateRoot = aggregateRoot;
                Timestamp = timestamp;
            }

            public AggregateRootCacheItem(IAggregateRoot aggregateRoot) : this(aggregateRoot, DateTime.UtcNow) { }

            #endregion

            #region Public Properties

            public IAggregateRoot AggregateRoot { get; set; }

            public DateTime Timestamp { get; set; }

            #endregion

            #region Public Methods

            public bool IsExpired(int expiration) => (DateTime.UtcNow - Timestamp).TotalSeconds >= expiration;

            #endregion
        }

        #endregion

        #region Private Fields

        readonly IRepository repository;
        readonly IBackgroundWorker backgroundWorker;
        readonly ILogger logger;
        readonly int expiration;
        readonly string backgroundWorkerKey;
        readonly ConcurrentDictionary<string, AggregateRootCacheItem> cacheItemMapping;
        bool disposed;
        bool started;

        #endregion

        #region Ctors

        public MemoryCache(IRepository repository, IBackgroundWorker backgroundWorker, ILogger<MemoryCache> logger, VoguediOptions options)
        {
            this.repository = repository;
            this.backgroundWorker = backgroundWorker;
            this.logger = logger;
            expiration = options.AggregateRootExpiration;
            backgroundWorkerKey = $"{nameof(MemoryCache)}_{SnowflakeId.Default().NewId()}";
            cacheItemMapping = new ConcurrentDictionary<string, AggregateRootCacheItem>();
        }

        #endregion

        #region Private Methods

        void Clear()
        {
            var aggregateRoots = new List<KeyValuePair<string, AggregateRootCacheItem>>();

            foreach (var item in cacheItemMapping)
            {
                if (item.Value.IsExpired(expiration))
                    aggregateRoots.Add(item);
            }

            foreach (var item in aggregateRoots)
            {
                if (cacheItemMapping.TryRemove(item.Key, out var cacheItem))
                    logger.LogDebug($"已过期聚合根清理成功。 [AggregateRootType = {cacheItem.AggregateRoot.GetType()}, AggregateRootId = {cacheItem.AggregateRoot.Id}, Expiration = {expiration}]");
            }
        }

        #endregion

        #region ICache

        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                    backgroundWorker.Stop(backgroundWorkerKey);

                disposed = true;
            }
        }

        async Task<TAggregateRoot> ICache.GetAsync<TAggregateRoot, TIdentity>(TIdentity aggregateRootId)
        {
            if (aggregateRootId == null)
                throw new ArgumentNullException(nameof(aggregateRootId));

            if (cacheItemMapping.TryGetValue(aggregateRootId.ToString(), out var cacheItem))
            {
                var aggregateRoot = cacheItem.AggregateRoot;

                if (aggregateRoot.GetUncommittedEvents()?.Count > 0)
                {
                    var lastedAggregateRoot = await repository.GetAsync<TAggregateRoot, TIdentity>(aggregateRootId);

                    if (lastedAggregateRoot != null)
                        await SetAsync(lastedAggregateRoot);

                    return lastedAggregateRoot;
                }

                return aggregateRoot as TAggregateRoot;
            }

            return null;
        }

        public Task SetAsync(IAggregateRoot aggregateRoot)
        {
            if (aggregateRoot == null)
                throw new ArgumentNullException(nameof(aggregateRoot));

            cacheItemMapping.AddOrUpdate(
                aggregateRoot.Id,
                new AggregateRootCacheItem(aggregateRoot),
                (key, cacheItem) =>
                {
                    cacheItem.AggregateRoot = aggregateRoot;
                    cacheItem.Timestamp = DateTime.UtcNow;
                    return cacheItem;
                });
            return Task.CompletedTask;
        }

        public async Task RefreshAsync(Type aggregateRootType, string aggregateRootId)
        {
            if (aggregateRootType == null)
                throw new ArgumentNullException(nameof(aggregateRootType));

            if (aggregateRootId == null)
                throw new ArgumentNullException(nameof(aggregateRootId));

            var aggregateRoot = await repository.GetAsync(aggregateRootType, aggregateRootId);
            
            if (aggregateRoot != null)
                await SetAsync(aggregateRoot);
        }

        public void Start()
        {
            if (!started)
            {
                backgroundWorker.Start(backgroundWorkerKey, Clear, expiration, expiration);
                started = true;
            }
        }

        #endregion
    }
}
