using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Voguedi.AsyncExecution;
using Voguedi.Domain.AggregateRoots;
using Voguedi.IdentityGeneration;
using Voguedi.ObjectSerialization;

namespace Voguedi.Domain.Caching.MemoryCache
{
    class MemoryCache : ICache
    {
        #region Private Fields

        readonly IDistributedCache cache;
        readonly IBinaryObjectSerializer objectSerializer;
        readonly ILogger logger;
        readonly MemoryCacheOptions options;
        readonly string baseKey = StringIdentityGenerator.Instance.Generate();

        #endregion

        #region Ctors

        public MemoryCache(IDistributedCache cache, IBinaryObjectSerializer objectSerializer, ILogger<MemoryCache> logger, MemoryCacheOptions options)
        {
            this.cache = cache;
            this.objectSerializer = objectSerializer;
            this.logger = logger;
            this.options = options;
        }

        #endregion

        #region Private Methods

        string BuildKey(string aggregateRootId) => $"{baseKey}_{aggregateRootId}";

        #endregion

        #region ICache

        public async Task<AsyncExecutedResult<IEventSourcedAggregateRoot>> GetAsync(Type aggregateRootType, string aggregateRootId)
        {
            try
            {
                var content = await cache.GetAsync(BuildKey(aggregateRootId));

                if (content != null)
                {
                    var obj = objectSerializer.Deserialize(content, aggregateRootType);

                    if (obj is IEventSourcedAggregateRoot aggregateRoot)
                    {
                        logger.LogInformation($"获取聚合根缓存成功！ [AggregateRootType = {aggregateRootType}, AggregateRootId = {aggregateRootId}]");
                        return AsyncExecutedResult<IEventSourcedAggregateRoot>.Success(aggregateRoot);
                    }
                }

                logger.LogError($"未获取聚合根缓存！ [AggregateRootType = {aggregateRootType}, AggregateRootId = {aggregateRootId}]");
                return AsyncExecutedResult<IEventSourcedAggregateRoot>.Success(null);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"未获取聚合根缓存！ [AggregateRootType = {aggregateRootType}, AggregateRootId = {aggregateRootId}]");
                return AsyncExecutedResult<IEventSourcedAggregateRoot>.Failed(ex);
            }
        }

        public async Task<AsyncExecutedResult> SetAsync(IEventSourcedAggregateRoot aggregateRoot)
        {
            var aggregateRootType = aggregateRoot.GetAggregateRootType();
            var aggregateRootId = aggregateRoot.GetAggregateRootId();

            try
            {
                await cache.SetAsync(
                    BuildKey(aggregateRootId),
                    objectSerializer.Serialize(aggregateRootType, aggregateRoot),
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpiration = options.AbsoluteExpiration,
                        AbsoluteExpirationRelativeToNow = options.AbsoluteExpirationRelativeToNow,
                        SlidingExpiration = options.SlidingExpiration
                    });
                logger.LogInformation($"更新聚合根缓存成功！ [AggregateRootType = {aggregateRootType}, AggregateRootId = {aggregateRootId}]");
                return AsyncExecutedResult.Success;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"更新聚合根缓存失败！ [AggregateRootType = {aggregateRootType}, AggregateRootId = {aggregateRootId}]");
                return AsyncExecutedResult.Failed(ex);
            }
        }

        #endregion
    }
}
