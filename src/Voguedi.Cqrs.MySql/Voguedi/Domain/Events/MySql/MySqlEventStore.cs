using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using MySql.Data.MySqlClient;
using Voguedi.Infrastructure;
using Voguedi.ObjectSerializers;

namespace Voguedi.Domain.Events.MySql
{
    class MySqlEventStore : IEventStore
    {
        #region Private Fields

        readonly IStringObjectSerializer objectSerializer;
        readonly string connectionString;
        readonly string schema;
        readonly string tableName;
        readonly int tableCount;
        const string versionUniqueIndexName = "IX_Events_AggregateRootId_Version";
        const string commandIdUniqueIndexName = "IX_Events_AggregateRootId_CommandId";
        const string getByCommandIdSql = "SELECT * FROM {0} WHERE `AggregateRootId` = @AggregateRootId AND `CommandId` = @CommandId";
        const string getByVersionSql = "SELECT * FROM {0} WHERE `AggregateRootId` = @AggregateRootId AND `Version` = @Version";
        const string getAllSql = "SELECT * FROM {0} WHERE `AggregateRootTypeName` = @AggregateRootTypeName AND `AggregateRootId` = @AggregateRootId AND `Version` >= @MinVersion AND `Version` <= @MaxVersion ORDER BY `Version` ASC";
        const string saveSql = "INSERT INTO {0} (`Id`, `Timestamp`, `CommandId`, `AggregateRootTypeName`, `AggregateRootId`, `Version`, `Events`) VALUES (@Id, @Timestamp, @CommandId, @AggregateRootTypeName, @AggregateRootId, @Version, @Events)";
        const string initializeSql = @"
            CREATE TABLE IF NOT EXISTS `{0}`.`{1}` (
                `Id` bigint NOT NULL,
                `Timestamp` datetime NOT NULL,
                `CommandId` bigint NOT NULL,
                `AggregateRootTypeName` varchar(256) NOT NULL,
                `AggregateRootId` varchar(32) NOT NULL,
                `Version` bigint NOT NULL,
                `Events` varchar(4000) NOT NULL,
                PRIMARY KEY (`Id`),
                UNIQUE KEY `IX_Events_AggregateRootId_Version` (`AggregateRootId`, `Version`),
                UNIQUE KEY `IX_Events_AggregateRootId_CommandId` (`AggregateRootId`, `CommandId`)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8";

        #endregion

        #region Ctors

        public MySqlEventStore(IStringObjectSerializer objectSerializer, MySqlOptions options)
        {
            this.objectSerializer = objectSerializer;
            connectionString = options.ConnectionString;
            schema = options.Schema;
            tableName = options.EventTableName;
            tableCount = options.EventTableCount;
        }

        #endregion

        #region Private Methods

        string BuildSql(string sql, string aggregateRootId)
        {
            if (tableCount > 1)
                return string.Format(sql, $"`{schema}`.`{tableName}_{Utils.GetServerKey(aggregateRootId, tableCount)}`");

            return string.Format(sql, $"`{schema}`.`{tableName}`");
        }

        EventStreamDescriptor ToStreamDescriptor(EventStream stream)
        {
            var eventContentMapping = new Dictionary<string, string>();

            foreach (var e in stream.Events)
                eventContentMapping.Add(e.GetTag(), objectSerializer.Serialize(e));

            return new EventStreamDescriptor
            {
                AggregateRootId = stream.AggregateRootId,
                AggregateRootTypeName = stream.AggregateRootTypeName,
                CommandId = stream.CommandId,
                Events = objectSerializer.Serialize(eventContentMapping),
                Id = stream.Id,
                Timestamp = stream.Timestamp,
                Version = stream.Version
            };
        }

        EventStream ToStream(EventStreamDescriptor streamDescriptor)
        {
            var events = new List<IEvent>();

            foreach (var item in objectSerializer.Deserialize<IDictionary<string, string>>(streamDescriptor.Events))
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
                using (var connection = new MySqlConnection(connectionString))
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
                return AsyncExecutedResult<EventStream>.Failed(ex);
            }
        }

        public async Task<AsyncExecutedResult<EventStream>> GetByVersionAsync(string aggregateRootId, long version)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
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
                using (var connection = new MySqlConnection(connectionString))
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
                return AsyncExecutedResult<IReadOnlyList<EventStream>>.Failed(ex);
            }
        }

        Task<AsyncExecutedResult<IReadOnlyList<EventStream>>> IEventStore.GetAllAsync<TAggregateRoot, TIdentity>(TIdentity aggregateRootId, long minVersion, long maxVersion)
            => GetAllAsync(typeof(TAggregateRoot).FullName, aggregateRootId.ToString(), minVersion, maxVersion);

        public async Task<AsyncExecutedResult<EventStreamSavedResult>> SaveAsync(EventStream stream)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.ExecuteAsync(BuildSql(saveSql, stream.AggregateRootId), ToStreamDescriptor(stream));
                    return AsyncExecutedResult<EventStreamSavedResult>.Success(EventStreamSavedResult.Success);
                }
            }
            catch (MySqlException ex)
            {
                if (ex.Number == 1062 && ex.Message.Contains(versionUniqueIndexName))
                    return AsyncExecutedResult<EventStreamSavedResult>.Success(EventStreamSavedResult.DuplicatedEvent);

                if (ex.Number == 1062 && ex.Message.Contains(commandIdUniqueIndexName))
                    return AsyncExecutedResult<EventStreamSavedResult>.Success(EventStreamSavedResult.DuplicatedCommand);

                return AsyncExecutedResult<EventStreamSavedResult>.Failed(ex, EventStreamSavedResult.Failed);
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
                var sql = new StringBuilder();

                if (tableCount > 1)
                {
                    for (var i = 0; i < tableCount; i++)
                        sql.AppendFormat(initializeSql, schema, $"{tableName}_{i}");
                }
                else
                    sql.AppendFormat(initializeSql, schema, tableName);

                using (var connection = new MySqlConnection(connectionString))
                    await connection.ExecuteAsync(sql.ToString());
            }
        }

        #endregion
    }
}
