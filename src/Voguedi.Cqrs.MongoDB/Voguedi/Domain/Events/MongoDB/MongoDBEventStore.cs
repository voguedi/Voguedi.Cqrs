using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Voguedi.AsyncExecution;

namespace Voguedi.Domain.Events.MongoDB
{
    class MongoDBEventStore : IEventStore
    {
        #region Private Fields

        readonly IMongoClient client;
        readonly IMongoDatabase database;
        readonly IClientSessionHandle session;
        readonly string collectionName;
        readonly IMongoCollection<EventStream> collection;
        readonly ILogger logger;

        #endregion

        #region Ctors

        public MongoDBEventStore(IServiceProvider serviceProvider, ILogger<MongoDBEventStore> logger, MongoDBOptions options)
        {
            client = serviceProvider.GetRequiredService<IMongoClient>();
            database = client.GetDatabase(options.DatabaseName);
            session = client.StartSession();
            session.StartTransaction();
            collectionName = options.EventCollectionName;
            collection = database.GetCollection<EventStream>(collectionName);
            this.logger = logger;
        }

        #endregion

        #region Private Methods

        IQueryable<EventStream> GetAll() => collection.AsQueryable();

        #endregion

        #region IEventStore

        public Task<AsyncExecutedResult<IReadOnlyList<EventStream>>> GetAllAsync(
            string aggregateRootTypeName,
            string aggregateRootId,
            long minVersion = -1L,
            long maxVersion = long.MaxValue)
        {
            try
            {
                var streams = GetAll().Where(e =>
                    e.AggregateRootTypeName == aggregateRootTypeName &&
                    e.AggregateRootId == aggregateRootId &&
                    e.Version >= minVersion &&
                    e.Version <= maxVersion);

                if (streams?.Count() > 0)
                {
                    logger.LogInformation($"获取事件成功！ EventStreams = [{streams.Select(s => s.ToString())}]");
                    return Task.FromResult(AsyncExecutedResult<IReadOnlyList<EventStream>>.Success(streams.ToList()));
                }

                logger.LogError($"未获取任何事件！ [AggregateRootTypeName = {aggregateRootTypeName}, AggregateRootId = {aggregateRootId}, MinVersion = {minVersion}, MaxVersion = {maxVersion}]");
                return Task.FromResult(AsyncExecutedResult<IReadOnlyList<EventStream>>.Success(null));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"未获取任何事件！ [AggregateRootTypeName = {aggregateRootTypeName}, AggregateRootId = {aggregateRootId}, MinVersion = {minVersion}, MaxVersion = {maxVersion}]");
                return Task.FromResult(AsyncExecutedResult<IReadOnlyList<EventStream>>.Failed(ex));
            }
}

        public Task<AsyncExecutedResult<EventStream>> GetByCommandIdAsync(string aggregateRootId, long commandId)
        {
            try
            {
                var stream = GetAll().FirstOrDefault(e => e.AggregateRootId == aggregateRootId && e.CommandId == commandId);

                if (stream != null)
                {
                    logger.LogInformation($"获取事件成功！ {stream}");
                    return Task.FromResult(AsyncExecutedResult<EventStream>.Success(stream));
                }

                logger.LogError($"未获取任何事件！ [AggregateRootId = {aggregateRootId}, CommandId = {commandId}]");
                return Task.FromResult(AsyncExecutedResult<EventStream>.Success(null));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"获取事件失败！ [AggregateRootId = {aggregateRootId}, CommandId = {commandId}]");
                return Task.FromResult(AsyncExecutedResult<EventStream>.Failed(ex));
            }
}

        public Task<AsyncExecutedResult<EventStream>> GetByVersionAsync(string aggregateRootId, long version)
        {
            try
            {
                var stream = GetAll().FirstOrDefault(e => e.AggregateRootId == aggregateRootId && e.Version == version);

                if (stream != null)
                {
                    logger.LogInformation($"获取事件成功！ {stream}");
                    return Task.FromResult(AsyncExecutedResult<EventStream>.Success(stream));
                }

                logger.LogError($"未获取任何事件！ [AggregateRootId = {aggregateRootId}, Version = {version}]");
                return Task.FromResult(AsyncExecutedResult<EventStream>.Success(null));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"未获取任何事件！ [AggregateRootId = {aggregateRootId}, Version = {version}]");
                return Task.FromResult(AsyncExecutedResult<EventStream>.Failed(ex));
            }
        }

        public async Task<AsyncExecutedResult<EventStreamSavedResult>> SaveAsync(EventStream stream)
        {
            if (GetAll().Any(e => e.AggregateRootId == stream.AggregateRootId && e.Version == stream.Version))
            {
                logger.LogWarning($"事件存储失败，存在相同版本！ {stream}");
                return AsyncExecutedResult<EventStreamSavedResult>.Success(EventStreamSavedResult.DuplicatedEvent);
            }

            if (GetAll().Any(e => e.AggregateRootId == stream.AggregateRootId && e.CommandId == stream.CommandId))
            {
                logger.LogWarning($"事件存储失败，存在相同命令！ {stream}");
                return AsyncExecutedResult<EventStreamSavedResult>.Success(EventStreamSavedResult.DuplicatedCommand);
            }

            try
            {
                await collection.InsertOneAsync(session, stream);
                await session.CommitTransactionAsync();
                logger.LogInformation($"事件存储成功！ {stream}");
                return AsyncExecutedResult<EventStreamSavedResult>.Success(EventStreamSavedResult.Success);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"事件存储失败！ {stream}");
                return AsyncExecutedResult<EventStreamSavedResult>.Failed(ex, EventStreamSavedResult.Failed);
            }
        }

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                var collectionNames = (await database.ListCollectionNamesAsync(cancellationToken: cancellationToken))?.ToList();

                if (collectionNames == null || collectionNames.Count == 0 || collectionNames.All(c => c != collectionName))
                    await database.CreateCollectionAsync(collectionName, cancellationToken: cancellationToken);
                
                logger.LogInformation($"事件存储器初始化成功！ [CollectionName = {collectionName}]");
            }
        }

        #endregion
    }
}
