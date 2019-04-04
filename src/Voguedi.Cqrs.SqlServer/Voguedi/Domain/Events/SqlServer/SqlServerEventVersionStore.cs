using System;
using System.Data.SqlClient;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Voguedi.Infrastructure;

namespace Voguedi.Domain.Events.SqlServer
{
    class SqlServerEventVersionStore : IEventVersionStore
    {
        #region Private Fields
        
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
	                EXEC('CREATE SCHEMA [{0}]'); 
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
                    ) ON [PRIMARY]; 
                END; ";

        #endregion

        #region Ctors

        public SqlServerEventVersionStore(SqlServerOptions options)
        {
            connectionString = options.ConnectionString;
            schema = options.Schema;
            tableName = options.EventVersionTableName;
            tableCount = options.EventVersionTableCount;
        }

        #endregion

        #region Private Methods

        string BuildSql(string sql, string aggregateRootId)
        {
            if (tableCount > 1)
                return string.Format(sql, $"[{schema}].[{tableName}_{Utils.GetServerKey(aggregateRootId, tableCount)}]");

            return string.Format(sql, $"[{schema}].[{tableName}]");
        }

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
                            Id = SnowflakeId.Default().NewId(),
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
                    for (var i = 0; i < tableCount; i++)
                        sql.AppendFormat(initializeSql, schema, $"{tableName}_{i}");
                }
                else
                    sql.AppendFormat(initializeSql, schema, tableName);

                using (var connection = new SqlConnection(connectionString))
                    await connection.ExecuteAsync(sql.ToString());
            }
        }

        #endregion
    }
}
