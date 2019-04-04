using System;
using System.Threading.Tasks;
using Voguedi.Cqrs.Samples.RabbitMQ.MemroyCache.SqlServer.Stores.DataObjects;
using Voguedi.Infrastructure;

namespace Voguedi.Cqrs.Samples.RabbitMQ.MemroyCache.SqlServer.Stores
{
    public interface INoteStore
    {
        #region Methods

        Task<AsyncExecutedResult> CreateAsync(string id, long version, string title, string content, DateTime createdOn);

        Task<AsyncExecutedResult> ModifyAsync(string id, long version, string title, string content, DateTime modifiedOn);

        Task<AsyncExecutedResult<NoteDataObject>> GetAsync(string id);

        #endregion
    }
}
