using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Voguedi.AsyncExecution;
using Voguedi.Messaging;
using Voguedi.ObjectSerialization;
using Voguedi.Utilities;

namespace Voguedi.Domain.Events.MySql
{
    class MySqlEventStore : IEventStore, IMessageStore
    {
        #region Private Fields

        readonly IStringObjectSerializer objectSerializer;
        readonly ILogger logger;
        readonly MySqlOptions options;
        const string tableName = "Events";
        const string versionUniqueIndexName = "IX_Events_AggregateRootId_Version";
        const string commandIdUniqueIndexName = "IX_Events_AggregateRootId_CommandId";
        const string getByCommandIdSql = "SELECT * FROM {0} WHERE `AggregateRootId` = @AggregateRootId AND `CommandId` = @CommandId";
        const string getByVersionSql = "SELECT * FROM {0} WHERE `AggregateRootId` = @AggregateRootId AND `Version` = @Version";
        const string getAllSql = "SELECT * FROM {0} WHERE `AggregateRootTypeName` = @AggregateRootTypeName AND `AggregateRootId` = @AggregateRootId AND `Version` >= @MinVersion AND `Version` <= @MaxVersion ORDER BY `Version` ASC";
        const string saveSql = "INSERT INTO {0} (`Id`, `Timestamp`, `CommandId`, `AggregateRootTypeName`, `AggregateRootId`, `Version`, `Events`) VALUES (@Id, @Timestamp, @CommandId, @AggregateRootTypeName, @AggregateRootId, @Version, @Events)";
        const string initializeSql = @"
            CREATE TABLE IF NOT EXISTS `{0}`.`{1}` (
                `Id` varchar(24) NOT NULL,
                `Timestamp` datetime NOT NULL,
                `CommandId` varchar(36) NOT NULL,
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

        public MySqlEventStore(IStringObjectSerializer objectSerializer, ILogger<MySqlEventStore> logger, MySqlOptions options)
        {
            this.objectSerializer = objectSerializer;
            this.logger = logger;
            this.options = options;
        }

        #endregion

        #region Private Methods

        string GetTableName(string aggregateRootId)
        {
            if (options.TableCount > 1)
            {
                var hashCode = Utils.GetHashCode(aggregateRootId);
                var tableNameIndex = hashCode & options.TableCount;
                return $"`{options.Schema}`.`{tableName}_{tableNameIndex}`";
            }

            return $"`{options.Schema}`.`{tableName}`";
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

        public async Task<AsyncExecutedResult<EventStream>> GetAsync(string aggregateRootId, string commandId)
        {
            if (string.IsNullOrWhiteSpace(aggregateRootId))
                throw new ArgumentNullException(nameof(aggregateRootId));

            if (string.IsNullOrWhiteSpace(commandId))
                throw new ArgumentNullException(nameof(commandId));

            try
            {
                using (var connection = new MySqlConnection(options.ConnectionString))
                {
                    var descriptor = await connection.QueryFirstOrDefaultAsync<EventStreamDescriptor>(
                        BuildSql(getByCommandIdSql, aggregateRootId),
                        new { AggregateRootId = aggregateRootId, CommandId = commandId });

                    if (descriptor != null)
                    {
                        var stream = ToStream(descriptor);
                        logger.LogInformation($"获取事件成功！ {stream}");
                        return AsyncExecutedResult<EventStream>.Success(stream);
                    }

                    logger.LogError($"未获取任何事件！ [AggregateRootId = {aggregateRootId}, CommandId = {commandId}]");
                    return AsyncExecutedResult<EventStream>.Success(null);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"获取事件失败！ [AggregateRootId = {aggregateRootId}, CommandId = {commandId}]");
                return AsyncExecutedResult<EventStream>.Failed(ex);
            }
        }

        public async Task<AsyncExecutedResult<EventStream>> GetAsync(string aggregateRootId, long version)
        {
            if (string.IsNullOrWhiteSpace(aggregateRootId))
                throw new ArgumentNullException(nameof(aggregateRootId));

            if (version < -1L)
                throw new ArgumentNullException(nameof(version));

            try
            {
                using (var connection = new MySqlConnection(options.ConnectionString))
                {
                    var descriptor = await connection.QueryFirstOrDefaultAsync<EventStreamDescriptor>(
                        BuildSql(getByVersionSql, aggregateRootId),
                        new { AggregateRootId = aggregateRootId, Version = version });

                    if (descriptor != null)
                    {
                        var stream = ToStream(descriptor);
                        logger.LogInformation($"获取事件成功！ {stream}");
                        return AsyncExecutedResult<EventStream>.Success(stream);
                    }

                    logger.LogError($"未获取任何事件！ [AggregateRootId = {aggregateRootId}, Version = {version}]");
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
            if (string.IsNullOrWhiteSpace(aggregateRootTypeName))
                throw new ArgumentNullException(nameof(aggregateRootTypeName));

            if (string.IsNullOrWhiteSpace(aggregateRootId))
                throw new ArgumentNullException(nameof(aggregateRootId));

            if (minVersion < -1L)
                throw new ArgumentOutOfRangeException(nameof(minVersion));

            if (maxVersion < -1L)
                throw new ArgumentOutOfRangeException(nameof(maxVersion));

            try
            {
                using (var connection = new MySqlConnection(options.ConnectionString))
                {
                    var descriptors = await connection.QueryAsync<EventStreamDescriptor>(
                        BuildSql(getAllSql, aggregateRootId),
                        new { AggregateRootTypeName = aggregateRootTypeName, AggregateRootId = aggregateRootId, MinVersion = minVersion, MaxVersion = maxVersion });

                    if (descriptors?.Count() > 0)
                    {
                        var streams = descriptors.Select(d => ToStream(d)).ToList();
                        logger.LogInformation($"获取事件成功！ EventStreams = [{streams.Select(s => s.ToString())}]");
                        return AsyncExecutedResult<IReadOnlyList<EventStream>>.Success(streams);
                    }

                    logger.LogError($"未获取任何事件！ [AggregateRootTypeName = {aggregateRootTypeName}, AggregateRootId = {aggregateRootId}, MinVersion = {minVersion}, MaxVersion = {maxVersion}]");
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
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            try
            {
                using (var connection = new MySqlConnection(options.ConnectionString))
                {
                    await connection.ExecuteAsync(BuildSql(saveSql, stream.AggregateRootId), ToStreamDescriptor(stream));
                    logger.LogInformation($"事件存储成功！ {stream}");
                    return AsyncExecutedResult<EventStreamSavedResult>.Success(EventStreamSavedResult.Success);
                }
            }
            catch (MySqlException ex)
            {
                if (ex.Number == 1062 && ex.Message.Contains(versionUniqueIndexName))
                {
                    logger.LogWarning(ex, $"事件存储失败，存在相同版本！ {stream}");
                    return AsyncExecutedResult<EventStreamSavedResult>.Success(EventStreamSavedResult.DuplicatedEvent);
                }
                else if (ex.Number == 1062 && ex.Message.Contains(commandIdUniqueIndexName))
                {
                    logger.LogWarning(ex, $"事件存储失败，存在相同命令！ {stream}");
                    return AsyncExecutedResult<EventStreamSavedResult>.Success(EventStreamSavedResult.DuplicatedCommand);
                }

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

        #region IMessageStore
        
        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                var sql = new StringBuilder();

                if (options.TableCount > 1)
                {
                    for (int i = 0, j = options.TableCount; i < j; i++)
                        sql.AppendFormat(initializeSql, options.Schema, $"{tableName}_{i}");
                }
                else
                    sql.AppendFormat(initializeSql, options.Schema, tableName);

                try
                {
                    using (var connection = new MySqlConnection(options.ConnectionString))
                        await connection.ExecuteAsync(sql.ToString());

                    logger.LogInformation($"事件存储器初始化成功！ [Sql = {initializeSql}]");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"事件存储器初始化失败！ [Sql = {initializeSql}]");
                }
            }
        }

        #endregion
    }
}
