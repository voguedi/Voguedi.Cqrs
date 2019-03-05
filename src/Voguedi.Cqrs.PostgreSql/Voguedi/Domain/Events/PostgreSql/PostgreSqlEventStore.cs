using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;
using Voguedi.AsyncExecution;
using Voguedi.ObjectSerializing;
using Voguedi.Stores;
using Voguedi.Utils;

namespace Voguedi.Domain.Events.PostgreSql
{
    class PostgreSqlEventStore : IEventStore, IStore
    {
        #region Private Fields

        readonly IStringObjectSerializer objectSerializer;
        readonly ILogger logger;
        readonly string connectionString;
        readonly string schema;
        readonly string tableName;
        readonly int tableCount;
        const string versionUniqueIndexName = "IX_Events_AggregateRootId_Version";
        const string commandIdUniqueIndexName = "IX_Events_AggregateRootId_CommandId";
        const string getByCommandIdSql = @"SELECT * FROM {0} WHERE ""AggregateRootId"" = @AggregateRootId AND ""CommandId"" = @CommandId";
        const string getByVersionSql = @"SELECT * FROM {0} WHERE ""AggregateRootId"" = @AggregateRootId AND ""Version"" = @Version";
        const string getAllSql = @"SELECT * FROM {0} WHERE ""AggregateRootTypeName"" = @AggregateRootTypeName AND ""AggregateRootId"" = @AggregateRootId AND ""Version"" >= @MinVersion AND ""Version"" <= @MaxVersion ORDER BY ""Version"" ASC";
        const string saveSql = @"INSERT INTO {0} (""Id"", ""Timestamp"", ""CommandId"", ""AggregateRootTypeName"", ""AggregateRootId"", ""Version"", ""Events"") VALUES (@Id, @Timestamp, @CommandId, @AggregateRootTypeName, @AggregateRootId, @Version, @Events)";
        const string initializeSql = @"
            CREATE SCHEMA IF NOT EXISTS ""{0}"";
            CREATE TABLE IF NOT EXISTS ""{0}"".""{1}""(
	            ""Id"" BIGINT PRIMARY KEY NOT NULL,
	            ""Timestamp"" TIMESTAMP NOT NULL,
	            ""CommandId"" BIGINT NOT NULL,
	            ""AggregateRootTypeName"" VARCHAR(256) NOT NULL,
	            ""AggregateRootId"" VARCHAR(32) NOT NULL,
	            ""Version"" BIGINT NOT NULL,
                ""Events"" VARCHAR(MAX) NOT NULL
            );";

        #endregion

        #region Ctors

        public PostgreSqlEventStore(IStringObjectSerializer objectSerializer, ILogger<PostgreSqlEventStore> logger, PostgreSqlOptions options)
        {
            this.objectSerializer = objectSerializer;
            this.logger = logger;
            connectionString = options.ConnectionString;
            schema = options.Schema;
            tableName = options.EventTableName;
            tableCount = options.EventTableCount;
        }

        #endregion

        #region Private Methods

        string GetTableName(string aggregateRootId)
        {
            if (tableCount > 1)
                return $@"""{schema}"".""{tableName}_{Helper.GetServerIndex(aggregateRootId, tableCount)}""";

            return $@"""{schema}"".""{tableName}""";
        }

        string BuildSql(string sql, string aggregateRootId) => string.Format(sql, GetTableName(aggregateRootId));

        EventStreamDescriptor ToStreamDescriptor(EventStream stream)
        {
            var eventContentMapping = new Dictionary<string, string>();

            foreach (var item in stream.Events)
                eventContentMapping.Add(item.GetType().AssemblyQualifiedName, objectSerializer.Serialize(item));

            var eventsContent = objectSerializer.Serialize(eventContentMapping);
            return new EventStreamDescriptor
            {
                AggregateRootId = stream.AggregateRootId,
                AggregateRootTypeName = stream.AggregateRootTypeName,
                CommandId = stream.CommandId,
                Events = eventsContent,
                Id = stream.Id,
                Timestamp = stream.Timestamp,
                Version = stream.Version
            };
        }

        EventStream ToStream(EventStreamDescriptor streamDescriptor)
        {
            var events = new List<IEvent>();
            var eventContentMapping = objectSerializer.Deserialize<IDictionary<string, string>>(streamDescriptor.Events);

            foreach (var item in eventContentMapping)
                events.Add((IEvent)objectSerializer.Deserialize(item.Value, Type.GetType(item.Key)));

            return new EventStream(
                streamDescriptor.Id,
                streamDescriptor.Timestamp,
                streamDescriptor.CommandId,
                streamDescriptor.AggregateRootTypeName,
                streamDescriptor.AggregateRootId,
                streamDescriptor.Version,
                events);
        }

        #endregion

        #region IEventStore

        public async Task<AsyncExecutedResult<EventStream>> GetByCommandIdAsync(string aggregateRootId, long commandId)
        {
            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    var descriptor = await connection.QueryFirstOrDefaultAsync<EventStreamDescriptor>(
                        BuildSql(getByCommandIdSql, aggregateRootId),
                        new { AggregateRootId = aggregateRootId, CommandId = commandId });

                    if (descriptor != null)
                        return AsyncExecutedResult<EventStream>.Success(ToStream(descriptor));

                    return AsyncExecutedResult<EventStream>.Success(null);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"获取事件失败！ [AggregateRootId = {aggregateRootId}, CommandId = {commandId}]");
                return AsyncExecutedResult<EventStream>.Failed(ex);
            }
        }

        public async Task<AsyncExecutedResult<EventStream>> GetByVersionAsync(string aggregateRootId, long version)
        {
            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    var descriptor = await connection.QueryFirstOrDefaultAsync<EventStreamDescriptor>(
                        BuildSql(getByVersionSql, aggregateRootId),
                        new { AggregateRootId = aggregateRootId, Version = version });

                    if (descriptor != null)
                        return AsyncExecutedResult<EventStream>.Success(ToStream(descriptor));

                    return AsyncExecutedResult<EventStream>.Success(null);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"未获取任何事件！ [AggregateRootId = {aggregateRootId}, Version = {version}]");
                return AsyncExecutedResult<EventStream>.Failed(ex);
            }
        }

        public async Task<AsyncExecutedResult<IReadOnlyList<EventStream>>> GetAllAsync(
            string aggregateRootTypeName,
            string aggregateRootId,
            long minVersion = -1L,
            long maxVersion = long.MaxValue)
        {
            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    var descriptors = await connection.QueryAsync<EventStreamDescriptor>(
                        BuildSql(getAllSql, aggregateRootId),
                        new { AggregateRootTypeName = aggregateRootTypeName, AggregateRootId = aggregateRootId, MinVersion = minVersion, MaxVersion = maxVersion });

                    if (descriptors?.Count() > 0)
                        return AsyncExecutedResult<IReadOnlyList<EventStream>>.Success(descriptors.Select(d => ToStream(d)).ToList());

                    return AsyncExecutedResult<IReadOnlyList<EventStream>>.Success(null);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"未获取任何事件！ [AggregateRootTypeName = {aggregateRootTypeName}, AggregateRootId = {aggregateRootId}, MinVersion = {minVersion}, MaxVersion = {maxVersion}]");
                return AsyncExecutedResult<IReadOnlyList<EventStream>>.Failed(ex);
            }
        }

        public async Task<AsyncExecutedResult<EventStreamSavedResult>> SaveAsync(EventStream stream)
        {
            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    await connection.ExecuteAsync(BuildSql(saveSql, stream.AggregateRootId), ToStreamDescriptor(stream));
                    return AsyncExecutedResult<EventStreamSavedResult>.Success(EventStreamSavedResult.Success);
                }
            }
            catch (NpgsqlException ex)
            {
                if (ex.ErrorCode == 23505 && ex.Message.Contains(versionUniqueIndexName))
                    return AsyncExecutedResult<EventStreamSavedResult>.Success(EventStreamSavedResult.DuplicatedEvent);

                if (ex.ErrorCode == 23505 && ex.Message.Contains(commandIdUniqueIndexName))
                    return AsyncExecutedResult<EventStreamSavedResult>.Success(EventStreamSavedResult.DuplicatedCommand);

                logger.LogError(ex, $"事件存储失败！ {stream}");
                return AsyncExecutedResult<EventStreamSavedResult>.Failed(ex, EventStreamSavedResult.Failed);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"事件存储失败！ {stream}");
                return AsyncExecutedResult<EventStreamSavedResult>.Failed(ex, EventStreamSavedResult.Failed);
            }
        }

        #endregion

        #region IStore

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                var sql = new StringBuilder();

                if (tableCount > 1)
                {
                    for (int i = 0, j = tableCount; i < j; i++)
                        sql.AppendFormat(initializeSql, schema, $"{tableName}_{i}");
                }
                else
                    sql.AppendFormat(initializeSql, schema, tableName);

                var count = 0;

                try
                {
                    using (var connection = new NpgsqlConnection(connectionString))
                        count = await connection.ExecuteAsync(sql.ToString());

                    if (count > 0)
                        logger.LogInformation($"事件存储器初始化成功！ [Sql = {sql}]");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"事件存储器初始化失败！ [Sql = {sql}]");
                }
            }
        }

        #endregion
    }
}
