using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Voguedi.BackgroundWorkers;
using Voguedi.DisposableObjects;
using Voguedi.Domain.AggregateRoots;
using Voguedi.Domain.Repositories;
using Voguedi.Services;
using Voguedi.Utils;

namespace Voguedi.Domain.Caching
{
    class MemoryCache : DisposableObject, ICache, IService
    {
        #region Private Class

        class AggregateRootCacheItem
        {
            #region Public Properties

            public IEventSourcedAggregateRoot AggregateRoot { get; set; }

            public DateTime Timestamp { get; set; }

            #endregion

            #region Ctors

            public AggregateRootCacheItem(IEventSourcedAggregateRoot aggregateRoot, DateTime timestamp)
            {
                AggregateRoot = aggregateRoot;
                Timestamp = timestamp;
            }

            public AggregateRootCacheItem(IEventSourcedAggregateRoot aggregateRoot) : this(aggregateRoot, DateTime.UtcNow) { }

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
        readonly ConcurrentDictionary<string, AggregateRootCacheItem> cacheItemMapping = new ConcurrentDictionary<string, AggregateRootCacheItem>();
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
            backgroundWorkerKey = $"{nameof(MemoryCache)}_{ObjectId.NewObjectId().ToString()}";
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

            var cacheItem = default(AggregateRootCacheItem);

            foreach (var item in aggregateRoots)
            {
                if (cacheItemMapping.TryRemove(item.Key, out cacheItem))
                    logger.LogInformation($"聚合根清理成功！ [AggregateRootType = {cacheItem.AggregateRoot.GetAggregateRootType()}, AggregateRootId = {cacheItem.AggregateRoot.GetAggregateRootId()}, Expiration = {expiration}]");
            }
        }

        #endregion

        #region DisposableObject

        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                    backgroundWorker.Stop(backgroundWorkerKey);

                disposed = true;
            }
        }

        #endregion

        #region ICache

        public async Task<IEventSourcedAggregateRoot> GetAsync(Type aggregateRootType, string aggregateRootId)
        {
            if (aggregateRootType == null)
                throw new ArgumentNullException(nameof(aggregateRootType));

            if (string.IsNullOrWhiteSpace(aggregateRootId))
                throw new ArgumentNullException(nameof(aggregateRootId));

            if (cacheItemMapping.TryGetValue(aggregateRootId, out var cacheItem))
            {
                var aggregateRoot = cacheItem.AggregateRoot;

                if (aggregateRoot.GetUncommittedEvents()?.Count > 0)
                {
                    var lastedAggregateRoot = await repository.GetAsync(aggregateRootType, aggregateRootId);

                    if (lastedAggregateRoot != null)
                        await SetAsync(lastedAggregateRoot);

                    return lastedAggregateRoot;
                }

                return aggregateRoot;
            }

            return null;
        }

        public Task SetAsync(IEventSourcedAggregateRoot aggregateRoot)
        {
            if (aggregateRoot == null)
                throw new ArgumentNullException(nameof(aggregateRoot));

            cacheItemMapping.AddOrUpdate(
                aggregateRoot.GetAggregateRootId(),
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

            if (string.IsNullOrWhiteSpace(aggregateRootId))
                throw new ArgumentNullException(nameof(aggregateRootId));

            var aggregateRoot = await repository.GetAsync(aggregateRootType, aggregateRootId);
            
            if (aggregateRoot != null)
                await SetAsync(aggregateRoot);
        }

        #endregion

        #region IService

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
