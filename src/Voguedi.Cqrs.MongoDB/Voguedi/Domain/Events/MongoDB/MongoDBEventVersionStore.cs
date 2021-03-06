﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Voguedi.Infrastructure;

namespace Voguedi.Domain.Events.MongoDB
{
    class MongoDBEventVersionStore : IEventVersionStore
    {
        #region Private Class

        class EventVersionDescriptor
        {
            #region Public Properties

            public long Id { get; set; }

            public string AggregateRootTypeName { get; set; }

            public string AggregateRootId { get; set; }

            public long Version { get; set; }

            public DateTime CreatedOn { get; set; }

            public DateTime? ModifiedOn { get; set; }

            #endregion
        }

        #endregion

        #region Private Fields

        readonly IMongoClient client;
        readonly IMongoDatabase database;
        readonly IClientSessionHandle session;
        readonly string collectionName;
        readonly IMongoCollection<EventVersionDescriptor> collection;

        #endregion

        #region Ctors

        public MongoDBEventVersionStore(IServiceProvider serviceProvider, MongoDBOptions options)
        {
            client = serviceProvider.GetRequiredService<IMongoClient>();
            database = client.GetDatabase(options.DatabaseName);
            session = client.StartSession();
            session.StartTransaction();
            collectionName = options.EventVersionCollectionName;
            collection = database.GetCollection<EventVersionDescriptor>(collectionName);
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
                    Id = SnowflakeId.Default().NewId(),
                    Version = 1
                };
                await collection.InsertOneAsync(session, descriptor);
                await session.CommitTransactionAsync();
                return AsyncExecutedResult.Success;
            }
            catch (Exception ex)
            {
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
                    await session.CommitTransactionAsync();
                }

                throw new Exception("未获取到任何事件版本。");
            }
            catch (Exception ex)
            {
                return AsyncExecutedResult.Failed(ex);
            }
        }

        #endregion

        #region IEventVersionStore

        public Task<AsyncExecutedResult<long>> GetAsync(string aggregateRootTypeName, string aggregateRootId)
        {
            try
            {
                var descriptor = GetAll().FirstOrDefault(d => d.AggregateRootTypeName == aggregateRootTypeName && d.AggregateRootId == aggregateRootId);

                if (descriptor != null)
                    return Task.FromResult(AsyncExecutedResult<long>.Success(descriptor.Version));

                return Task.FromResult(AsyncExecutedResult<long>.Success(0));
            }
            catch (Exception ex)
            {
                return Task.FromResult(AsyncExecutedResult<long>.Failed(ex));
            }
        }

        public Task<AsyncExecutedResult> SaveAsync(string aggregateRootTypeName, string aggregateRootId, long version)
        {
            if (version == 1L)
                return CreateAsync(aggregateRootTypeName, aggregateRootId);

            return ModifyAsync(aggregateRootTypeName, aggregateRootId, version);
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
