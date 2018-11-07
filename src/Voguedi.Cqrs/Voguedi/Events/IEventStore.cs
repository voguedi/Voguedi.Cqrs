using System.Collections.Generic;
using System.Threading.Tasks;
using Voguedi.AsyncExecution;
using Voguedi.Stores;

namespace Voguedi.Events
{
    public interface IEventStore : IStore
    {
        #region Methods

        Task<AsyncExecutedResult<EventStreamSavedResult>> SaveAsync(EventStream stream);

        Task<AsyncExecutedResult<EventStream>> GetAsync(string aggregateRootId, string commandId);

        Task<AsyncExecutedResult<EventStream>> GetAsync(string aggregateRootId, long version);

        Task<AsyncExecutedResult<IReadOnlyList<EventStream>>> GetAllAsync(
            string aggregateRootTypeName,
            string aggregateRootId,
            long minVersion = -1L,
            long maxVersion = long.MaxValue);

        #endregion
    }
}
