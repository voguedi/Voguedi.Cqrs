using System;
using System.Data.SqlClient;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Voguedi.AsyncExecution;
using Voguedi.IdentityGeneration;
using Voguedi.Stores;
using Voguedi.Utilities;

namespace Voguedi.Domain.Events.PostgreSql
{
    class PostgreSqlEventVersionStore : IEventVersionStore, IStore
    {
        #region Private Fields

        readonly IStringIdentityGenerator identityGenerator;
        readonly ILogger logger;
        readonly string connectionString;
        readonly string schema;
        readonly string tableName;
        readonly int tableCount;
        const string getSql = @"SELECT ""Version"" FROM {0} WHERE ""AggregateRootTypeName"" = @AggregateRootTypeName AND ""AggregateRootId"" = @AggregateRootId";
        const string createSql = @"INSERT INTO {0} (""Id"", ""AggregateRootTypeName"", ""AggregateRootId"", ""Version"", ""CreatedOn"") VALUES (@Id, @AggregateRootTypeName, @AggregateRootId, @Version, @CreatedOn)";
        const string modifySql = @"UPDATE {0} SET ""Version"" = @Version, ""ModifiedOn"" = @ModifiedOn WHERE ""AggregateRootTypeName"" = @AggregateRootTypeName AND ""AggregateRootId"" = @AggregateRootId AND ""Version"" = (@Version - 1)";
        const string initializeSql = @"
            CREATE SCHEMA IF NOT EXISTS ""{0}"";
            CREATE TABLE IF NOT EXISTS ""{0}"".""{1}""(
	            ""Id"" VARCHAR(24) PRIMARY KEY NOT NULL,
	            ""AggregateRootTypeName"" VARCHAR(256) NOT NULL,
	            ""AggregateRootId"" VARCHAR(32) NOT NULL,
	            ""Version"" BIGINT NOT NULL,
	            ""CreatedOn"" TIMESTAMP NOT NULL,
	            ""ModifiedOn"" TIMESTAMP NULL,
            );";

        #endregion

        #region Ctors

        public PostgreSqlEventVersionStore(IStringIdentityGenerator identityGenerator, ILogger<PostgreSqlEventVersionStore> logger, PostgreSqlOptions options)
        {
            this.identityGenerator = identityGenerator;
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
            {
                var hashCode = Utils.GetHashCode(aggregateRootId);
                var tableNameIndex = hashCode & tableCount;
                return $@"""{schema}"".""{tableName}_{tableNameIndex}""";
            }

            return $@"""{schema}"".""{tableName}""";
        }

        string BuildSql(string sql, string aggregateRootId) => string.Format(sql, GetTableName(aggregateRootId));

        async Task<AsyncExecutedResult> CreateAsync(string aggregateRootTypeName, string aggregateRootId)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
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
                using (var connection = new SqlConnection(connectionString))
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
            if (string.IsNullOrWhiteSpace(aggregateRootTypeName))
                throw new ArgumentNullException(nameof(aggregateRootTypeName));

            if (string.IsNullOrWhiteSpace(aggregateRootId))
                throw new ArgumentNullException(nameof(aggregateRootId));

            try
            {
                using (var connection = new SqlConnection(connectionString))
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
                    using (var connection = new SqlConnection(connectionString))
                        count = await connection.ExecuteAsync(sql.ToString());

                    if (count > 0)
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
