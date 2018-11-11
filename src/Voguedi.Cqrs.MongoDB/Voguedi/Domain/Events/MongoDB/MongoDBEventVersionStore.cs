using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Voguedi.AsyncExecution;
using Voguedi.IdentityGeneration;
using Voguedi.MongoDB;

namespace Voguedi.Domain.Events.MongoDB
{
    class MongoDBEventVersionStore : IEventVersionStore
    {
        #region Private Class

        class EventVersionDescriptor
        {
            #region Public Properties

            public string Id { get; set; }

            public string AggregateRootTypeName { get; set; }

            public string AggregateRootId { get; set; }

            public long Version { get; set; }

            public DateTime CreatedOn { get; set; }

            public DateTime? ModifiedOn { get; set; }

            #endregion
        }

        #endregion

        #region Private Fields

        readonly IMongoDBContext dbContext;
        readonly IStringIdentityGenerator identityGenerator;
        readonly ILogger logger;
        readonly IMongoCollection<EventVersionDescriptor> collection;
        readonly IClientSessionHandle session;

        #endregion

        #region Ctors

        public MongoDBEventVersionStore(IMongoDBContext dbContext, IStringIdentityGenerator identityGenerator, ILogger<MongoDBEventVersionStore> logger)
        {
            this.dbContext = dbContext;
            this.identityGenerator = identityGenerator;
            collection = dbContext.Database.GetCollection<EventVersionDescriptor>("EventVersions");
            session = dbContext.Session;
            this.logger = logger;
        }

        #endregion

        #region Private Methods

        IQueryable<EventVersionDescriptor> GetAll() => collection.AsQueryable();

        async Task<AsyncExecutedResult> CreateAsync(string aggregateRootTypeName, string aggregateRootId)
        {
            try
            {
                var descriptor = new EventVersionDescriptor
                {
                    AggregateRootId = aggregateRootId,
                    AggregateRootTypeName = aggregateRootTypeName,
                    CreatedOn = DateTime.UtcNow,
                    Id = identityGenerator.Generate(),
                    Version = 1
                };
                await collection.InsertOneAsync(session, descriptor);
                await dbContext.SaveChangesAsync();
                logger.LogInformation($"存储已发布事件版本成功！ [AggregateRootTypeName = {aggregateRootTypeName}, AggregateRootId = {aggregateRootId}, Version = 1]");
                return AsyncExecutedResult.Success;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"存储已发布事件版本失败！ [AggregateRootTypeName = {aggregateRootTypeName}, AggregateRootId = {aggregateRootId}, Version = 1]");
                return AsyncExecutedResult.Failed(ex);
            }
        }

        async Task<AsyncExecutedResult> ModifyAsync(string aggregateRootTypeName, string aggregateRootId, long version)
        {
            try
            {
                var descriptor = GetAll().FirstOrDefault(e => e.AggregateRootTypeName == aggregateRootTypeName && e.AggregateRootId == aggregateRootId);

                if (descriptor != null)
                {
                    descriptor.Version = version;
                    var filter = Builders<EventVersionDescriptor>.Filter;
                    var specification = filter.Eq(e => e.AggregateRootTypeName, aggregateRootTypeName) & filter.Eq(e => e.AggregateRootId, aggregateRootId);
                    await collection.ReplaceOneAsync(session, specification, descriptor);
                    await dbContext.SaveChangesAsync();
                }

                throw new Exception("未获取任何已发布事件版本！");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"存储已发布事件版本失败！ [AggregateRootTypeName = {aggregateRootTypeName}, AggregateRootId = {aggregateRootId}, Version = {version}]");
                return AsyncExecutedResult.Failed(ex);
            }
        }

        #endregion

        #region IEventVersionStore

        public Task<AsyncExecutedResult<long>> GetAsync(string aggregateRootTypeName, string aggregateRootId)
        {
            if (string.IsNullOrWhiteSpace(aggregateRootTypeName))
                throw new ArgumentNullException(nameof(aggregateRootTypeName));

            if (string.IsNullOrWhiteSpace(aggregateRootId))
                throw new ArgumentNullException(nameof(aggregateRootId));

            try
            {
                var descriptor = GetAll().FirstOrDefault(d => d.AggregateRootTypeName == aggregateRootTypeName && d.AggregateRootId == aggregateRootId);

                if (descriptor != null)
                {
                    logger.LogInformation($"获取已发布事件版本成功！ [AggregateRootTypeName = {aggregateRootTypeName}, AggregateRootId = {aggregateRootId}, Version = {descriptor.Version}]");
                    return Task.FromResult(AsyncExecutedResult<long>.Success(descriptor.Version));
                }

                logger.LogInformation($"未获取任何已发布事件版本！ [AggregateRootTypeName = {aggregateRootTypeName}, AggregateRootId = {aggregateRootId}]");
                return Task.FromResult(AsyncExecutedResult<long>.Success(0));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"获取已发布事件版本失败！ [AggregateRootTypeName = {aggregateRootTypeName}, AggregateRootId = {aggregateRootId}]");
                return Task.FromResult(AsyncExecutedResult<long>.Failed(ex));
            }
        }

        public Task<AsyncExecutedResult> SaveAsync(string aggregateRootTypeName, string aggregateRootId, long version)
        {
            if (string.IsNullOrWhiteSpace(aggregateRootTypeName))
                throw new ArgumentNullException(nameof(aggregateRootTypeName));

            if (string.IsNullOrWhiteSpace(aggregateRootId))
                throw new ArgumentNullException(nameof(aggregateRootId));

            if (version < -1L)
                throw new ArgumentOutOfRangeException(nameof(version));

            if (version == 1L)
                return CreateAsync(aggregateRootTypeName, aggregateRootId);

            return ModifyAsync(aggregateRootTypeName, aggregateRootId, version);
        }

        #endregion
    }
}
