using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Voguedi.AsyncExecution;
using Voguedi.IdentityGeneration;
using Voguedi.Stores;
using Voguedi.Utilities;

namespace Voguedi.Domain.Events.MySql
{
    class MySqlEventVersionStore : IEventVersionStore, IStore
    {
        #region Private Fields

        readonly IStringIdentityGenerator identityGenerator;
        readonly ILogger logger;
        readonly MySqlOptions options;
        const string tableName = "EventVersions";
        const string getSql = "SELECT `Version` FROM {0} WHERE `AggregateRootTypeName` = @AggregateRootTypeName AND `AggregateRootId` = @AggregateRootId";
        const string createSql = "INSERT INTO {0} (`Id`, `AggregateRootTypeName`, `AggregateRootId`, `Version`, `CreatedOn`) VALUES (@Id, @AggregateRootTypeName, @AggregateRootId, @Version, @CreatedOn)";
        const string modifySql = "UPDATE {0} SET `Version` = @Version, `ModifiedOn` = @ModifiedOn WHERE `AggregateRootTypeName` = @AggregateRootTypeName AND `AggregateRootId` = @AggregateRootId AND `Version` = (@Version - 1)";
        const string initializeSql = @"
            CREATE TABLE IF NOT EXISTS `{0}`.`{1}` (
                `Id` varchar(24) NOT NULL,
                `AggregateRootTypeName` varchar(256) NOT NULL,
                `AggregateRootId` varchar(32) NOT NULL,
                `Version` bigint NOT NULL,
                `CreatedOn` datetime NOT NULL,
                `ModifiedOn` datetime DEFAULT NULL,
                PRIMARY KEY (`Id`)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8";

        #endregion

        #region Ctors

        public MySqlEventVersionStore(IStringIdentityGenerator identityGenerator, ILogger<MySqlEventVersionStore> logger, MySqlOptions options)
        {
            this.identityGenerator = identityGenerator;
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

        async Task<AsyncExecutedResult> CreateAsync(string aggregateRootTypeName, string aggregateRootId)
        {
            try
            {
                using (var connection = new MySqlConnection(options.ConnectionString))
                {
                    await connection.ExecuteAsync(
                        BuildSql(createSql, aggregateRootId),
                        new
                        {
                            Id = identityGenerator.Generate(),
                            AggregateRootTypeName = aggregateRootTypeName,
                            AggregateRootId = aggregateRootId,
                            Version = 1L,
                            CreatedOn = DateTime.UtcNow
                        });
                    logger.LogInformation($"存储已发布事件版本成功！ [AggregateRootTypeName = {aggregateRootTypeName}, AggregateRootId = {aggregateRootId}, Version = 1]");
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
                using (var connection = new MySqlConnection(options.ConnectionString))
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
                    logger.LogInformation($"存储已发布事件版本成功！ [AggregateRootTypeName = {aggregateRootTypeName}, AggregateRootId = {aggregateRootId}, Version = {version}]");
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
            if (string.IsNullOrWhiteSpace(aggregateRootTypeName))
                throw new ArgumentNullException(nameof(aggregateRootTypeName));

            if (string.IsNullOrWhiteSpace(aggregateRootId))
                throw new ArgumentNullException(nameof(aggregateRootId));

            try
            {
                using (var connection = new MySqlConnection(options.ConnectionString))
                {
                    var version = await connection.QueryFirstOrDefaultAsync<long>(
                        BuildSql(getSql, aggregateRootId),
                        new { AggregateRootTypeName = aggregateRootTypeName, AggregateRootId = aggregateRootId });
                    logger.LogInformation($"获取已发布事件版本成功！ [AggregateRootTypeName = {aggregateRootTypeName}, AggregateRootId = {aggregateRootId}, Version = {version}]");
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

        #region IStore

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

                    logger.LogInformation($"已发布事件版本存储器初始化成功！ [Sql = {initializeSql}]");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"已发布事件版本存储器初始化失败！ [Sql = {initializeSql}]");
                }
            }
        }

        #endregion
    }
}
