using System;
using System.Text;
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
        readonly IStringObjectSerializer objectSerializer;
        readonly ILogger logger;
        readonly MemoryCacheOptions options;
        readonly Encoding encoding;
        readonly string baseKey;

        #endregion

        #region Ctors

        public MemoryCache(IDistributedCache cache, IStringObjectSerializer objectSerializer, IStringIdentityGenerator identityGenerator, ILogger<MemoryCache> logger, MemoryCacheOptions options)
        {
            this.cache = cache;
            this.objectSerializer = objectSerializer;
            this.logger = logger;
            this.options = options;
            encoding = Encoding.UTF8;
            baseKey = identityGenerator.Generate();
        }

        #endregion

        #region Private Methods

        string BuildKey(string aggregateRootId) => $"{baseKey}_{aggregateRootId}";

        #endregion

        #region ICache

        public async Task<AsyncExecutedResult<IEventSourcedAggregateRoot>> GetAsync(Type aggregateRootType, string aggregateRootId)
        {
            if (aggregateRootType == null)
                throw new ArgumentNullException(nameof(aggregateRootType));

            if (string.IsNullOrWhiteSpace(aggregateRootId))
                throw new ArgumentNullException(nameof(aggregateRootId));

            try
            {
                var content = await cache.GetAsync(BuildKey(aggregateRootId));

                if (content != null)
                {
                    var obj = objectSerializer.Deserialize(encoding.GetString(content), aggregateRootType);

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
            if (aggregateRoot == null)
                throw new ArgumentNullException(nameof(aggregateRoot));

            var aggregateRootType = aggregateRoot.GetAggregateRootType();
            var aggregateRootId = aggregateRoot.GetAggregateRootId();

            try
            {
                await cache.SetAsync(
                    BuildKey(aggregateRootId),
                    encoding.GetBytes(objectSerializer.Serialize(aggregateRootType, aggregateRoot)),
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
