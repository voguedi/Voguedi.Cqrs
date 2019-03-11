using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Voguedi.AsyncExecution;
using Voguedi.Utils;

namespace Voguedi.Domain.Events.MySql
{
    class MySqlEventVersionStore : IEventVersionStore
    {
        #region Private Fields
        
        readonly ILogger logger;
        readonly string connectionString;
        readonly string schema;
        readonly string tableName;
        readonly int tableCount;
        const string getSql = "SELECT `Version` FROM {0} WHERE `AggregateRootTypeName` = @AggregateRootTypeName AND `AggregateRootId` = @AggregateRootId";
        const string createSql = "INSERT INTO {0} (`Id`, `AggregateRootTypeName`, `AggregateRootId`, `Version`, `CreatedOn`) VALUES (@Id, @AggregateRootTypeName, @AggregateRootId, @Version, @CreatedOn)";
        const string modifySql = "UPDATE {0} SET `Version` = @Version, `ModifiedOn` = @ModifiedOn WHERE `AggregateRootTypeName` = @AggregateRootTypeName AND `AggregateRootId` = @AggregateRootId AND `Version` = (@Version - 1)";
        const string initializeSql = @"
            CREATE TABLE IF NOT EXISTS {0} (
                `Id` bigint NOT NULL,
                `AggregateRootTypeName` varchar(256) NOT NULL,
                `AggregateRootId` varchar(32) NOT NULL,
                `Version` bigint NOT NULL,
                `CreatedOn` datetime NOT NULL,
                `ModifiedOn` datetime DEFAULT NULL,
                PRIMARY KEY (`Id`)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8";

        #endregion

        #region Ctors

        public MySqlEventVersionStore(ILogger<MySqlEventVersionStore> logger, MySqlOptions options)
        {
            this.logger = logger;
            connectionString = options.ConnectionString;
            schema = options.Schema;
            tableName = options.EventVersionTableName;
            tableCount = options.EventVersionTableCount;
        }

        #endregion

        #region Private Methods

        string GetTableName(string aggregateRootId)
        {
            if (tableCount > 1)
                return $"`{schema}`.`{tableName}_{Helper.GetServerIndex(aggregateRootId, tableCount)}`";

            return $"`{schema}`.`{tableName}`";
        }

        string BuildSql(string sql, string aggregateRootId) => string.Format(sql, GetTableName(aggregateRootId));

        async Task<AsyncExecutedResult> CreateAsync(string aggregateRootTypeName, string aggregateRootId)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.ExecuteAsync(
                        BuildSql(createSql, aggregateRootId),
                        new
                        {
                            Id = SnowflakeId.Instance.NewId(),
                            AggregateRootTypeName = aggregateRootTypeName,
                            AggregateRootId = aggregateRootId,
                            Version = 1L,
                            CreatedOn = DateTime.UtcNow
                        });
                    return AsyncExecutedResult.Success;
                }
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
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.ExecuteAsync(
                        BuildSql(modifySql, aggregateRootId),
                        new
                        {
                            AggregateRootTypeName = aggregateRootTypeName,
                            AggregateRootId = aggregateRootId,
                            Version = version,
                            ModifiedOn = DateTime.UtcNow
                        });
                    return AsyncExecutedResult.Success;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"存储已发布事件版本失败！ [AggregateRootTypeName = {aggregateRootTypeName}, AggregateRootId = {aggregateRootId}, Version = {version}]");
                return AsyncExecutedResult.Failed(ex);
            }
        }

        #endregion

        #region IEventVersionStore

        public async Task<AsyncExecutedResult<long>> GetAsync(string aggregateRootTypeName, string aggregateRootId)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    var version = await connection.QueryFirstOrDefaultAsync<long>(
                        BuildSql(getSql, aggregateRootId),
                        new { AggregateRootTypeName = aggregateRootTypeName, AggregateRootId = aggregateRootId });
                    return AsyncExecutedResult<long>.Success(version);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"获取已发布事件版本失败！ [AggregateRootTypeName = {aggregateRootTypeName}, AggregateRootId = {aggregateRootId}]");
                return AsyncExecutedResult<long>.Failed(ex);
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
                var sql = new StringBuilder();

                if (tableCount > 1)
                {
                    for (int i = 0, j = tableCount; i < j; i++)
                        sql.AppendFormat(initializeSql, $"`{schema}`.`{tableName}_{i}`");
                }
                else
                    sql.AppendFormat(initializeSql, $"`{schema}`.`{tableName}`");

                try
                {
                    using (var connection = new MySqlConnection(connectionString))
                        await connection.ExecuteAsync(sql.ToString());

                    logger.LogInformation($"已发布事件版本存储器初始化成功！ [Sql = {sql}]");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"已发布事件版本存储器初始化失败！ [Sql = {sql}]");
                }
            }
        }

        #endregion
    }
}
