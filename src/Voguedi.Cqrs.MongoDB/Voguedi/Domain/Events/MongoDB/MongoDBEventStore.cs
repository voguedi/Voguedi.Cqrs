using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Voguedi.Infrastructure;

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

        #endregion

        #region Ctors

        public MongoDBEventStore(IServiceProvider serviceProvider, MongoDBOptions options)
        {
            client = serviceProvider.GetRequiredService<IMongoClient>();
            database = client.GetDatabase(options.DatabaseName);
            session = client.StartSession();
            session.StartTransaction();
            collectionName = options.EventCollectionName;
            collection = database.GetCollection<EventStream>(collectionName);
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
                    return Task.FromResult(AsyncExecutedResult<IReadOnlyList<EventStream>>.Success(streams.ToList()));

                return Task.FromResult(AsyncExecutedResult<IReadOnlyList<EventStream>>.Success(null));
            }
            catch (Exception ex)
            {
                return Task.FromResult(AsyncExecutedResult<IReadOnlyList<EventStream>>.Failed(ex));
            }
        }

        Task<AsyncExecutedResult<IReadOnlyList<EventStream>>> IEventStore.GetAllAsync<TAggregateRoot, TIdentity>(TIdentity aggregateRootId, long minVersion, long maxVersion)
            => GetAllAsync(typeof(TAggregateRoot).FullName, aggregateRootId.ToString(), minVersion, maxVersion);

        public Task<AsyncExecutedResult<EventStream>> GetByCommandIdAsync(string aggregateRootId, long commandId)
        {
            try
            {
                var stream = GetAll().FirstOrDefault(e => e.AggregateRootId == aggregateRootId && e.CommandId == commandId);

                if (stream != null)
                    return Task.FromResult(AsyncExecutedResult<EventStream>.Success(stream));

                return Task.FromResult(AsyncExecutedResult<EventStream>.Success(null));
            }
            catch (Exception ex)
            {
                return Task.FromResult(AsyncExecutedResult<EventStream>.Failed(ex));
            }
}

        public Task<AsyncExecutedResult<EventStream>> GetByVersionAsync(string aggregateRootId, long version)
        {
            try
            {
                var stream = GetAll().FirstOrDefault(e => e.AggregateRootId == aggregateRootId && e.Version == version);

                if (stream != null)
                    return Task.FromResult(AsyncExecutedResult<EventStream>.Success(stream));

                return Task.FromResult(AsyncExecutedResult<EventStream>.Success(null));
            }
            catch (Exception ex)
            {
                return Task.FromResult(AsyncExecutedResult<EventStream>.Failed(ex));
            }
        }

        public async Task<AsyncExecutedResult<EventStreamSavedResult>> SaveAsync(EventStream stream)
        {
            if (GetAll().Any(e => e.AggregateRootId == stream.AggregateRootId && e.Version == stream.Version))
                return AsyncExecutedResult<EventStreamSavedResult>.Success(EventStreamSavedResult.DuplicatedEvent);

            if (GetAll().Any(e => e.AggregateRootId == stream.AggregateRootId && e.CommandId == stream.CommandId))
                return AsyncExecutedResult<EventStreamSavedResult>.Success(EventStreamSavedResult.DuplicatedCommand);

            try
            {
                await collection.InsertOneAsync(session, stream);
                await session.CommitTransactionAsync();
                return AsyncExecutedResult<EventStreamSavedResult>.Success(EventStreamSavedResult.Success);
            }
            catch (Exception ex)
            {
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
            }
        }

        #endregion
    }
}
