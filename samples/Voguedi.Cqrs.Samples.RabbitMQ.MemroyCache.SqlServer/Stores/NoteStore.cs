using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;
using Voguedi.AsyncExecution;
using Voguedi.Cqrs.Samples.RabbitMQ.MemroyCache.SqlServer.Stores.DataObjects;

namespace Voguedi.Cqrs.Samples.RabbitMQ.MemroyCache.SqlServer.Stores
{
    class NoteStore : INoteStore
    {
        #region Private Fields

        readonly string connectionString;
        const string createSql = "INSERT INTO Notes(Id, Version, Title, Content, CreatedOn) VALUES(@Id, @Version, @Title, @Content, @CreatedOn)";
        const string modifySql = "UPDATE Notes SET Title = @Title, Content = @Content, Version = @Version, ModifiedOn = @ModifiedOn WHERE Version = (@Version - 1) AND Id = @Id";
        const string getSql = "SELECT * FROM Notes WHERE Id = @Id";

        #endregion

        #region Ctors

        public NoteStore(string connectionString) => this.connectionString = connectionString;

        #endregion

        #region INoteStore

        public async Task<AsyncExecutedResult> CreateAsync(string id, long version, string title, string content, DateTime createdOn)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.ExecuteAsync(createSql, new { Id = id, Version = version, Title = title, Content = content, CreatedOn = createdOn });
                    return AsyncExecutedResult.Success;
                }
            }
            catch (Exception ex)
            {
                return AsyncExecutedResult.Failed(ex);
            }
        }

        public async Task<AsyncExecutedResult> ModifyAsync(string id, long version, string title, string content, DateTime modifiedOn)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.ExecuteAsync(modifySql, new { Id = id, Version = version, Title = title, Content = content, ModifiedOn = modifiedOn });
                    return AsyncExecutedResult.Success;
                }
            }
            catch (Exception ex)
            {
                return AsyncExecutedResult.Failed(ex);
            }
        }

        public async Task<AsyncExecutedResult<NoteDataObject>> GetAsync(string id)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    var dataObject = await connection.QueryFirstOrDefaultAsync<NoteDataObject>(getSql, new { Id = id });
                    return AsyncExecutedResult<NoteDataObject>.Success(dataObject);
                }
            }
            catch (Exception ex)
            {
                return AsyncExecutedResult<NoteDataObject>.Failed(ex);
            }
        }

        #endregion
    }
}
