using System;
using System.Data.SqlClient;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Voguedi.AsyncExecution;
using Voguedi.Stores;
using Voguedi.Utils;

namespace Voguedi.Domain.Events.SqlServer
{
    class SqlServerEventVersionStore : IEventVersionStore, IStore
    {
        #region Private Fields
        
        readonly ILogger logger;
        readonly string connectionString;
        readonly string schema;
        readonly string tableName;
        readonly int tableCount;
        const string getSql = "SELECT [Version] FROM {0} WHERE [AggregateRootTypeName] = @AggregateRootTypeName AND [AggregateRootId] = @AggregateRootId";
        const string createSql = "INSERT INTO {0} ([Id], [AggregateRootTypeName], [AggregateRootId], [Version], [CreatedOn]) VALUES (@Id, @AggregateRootTypeName, @AggregateRootId, @Version, @CreatedOn)";
        const string modifySql = "UPDATE {0} SET [Version] = @Version, [ModifiedOn] = @ModifiedOn WHERE [AggregateRootTypeName] = @AggregateRootTypeName AND [AggregateRootId] = @AggregateRootId AND [Version] = (@Version - 1)";
        const string initializeSql = @"
            IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = '{0}')
            BEGIN
	            EXEC('CREATE SCHEMA [{0}]')
            END;
            IF OBJECT_ID(N'[{0}].[{1}]',N'U') IS NULL
            BEGIN
                CREATE TABLE [{0}].[{1}](
	                [Id] [bigint] NOT NULL,
	                [AggregateRootTypeName] [varchar](256) NOT NULL,
	                [AggregateRootId] [varchar](32) NOT NULL,
	                [Version] [bigint] NOT NULL,
	                [CreatedOn] [datetime] NOT NULL,
	                [ModifiedOn] [datetime] NULL,
                    CONSTRAINT [PK_{1}] PRIMARY KEY CLUSTERED(
	                    [Id] ASC
                    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
                ) ON [PRIMARY]
            END";

        #endregion

        #region Ctors

        public SqlServerEventVersionStore(ILogger<SqlServerEventVersionStore> logger, SqlServerOptions options)
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
                return $"[{schema}].[{tableName}_{Helper.GetServerIndex(aggregateRootId, tableCount)}]";

            return $"[{schema}].[{tableName}]";
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
            try
            {
                using (var connection = new SqlConnection(connectionString))
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
