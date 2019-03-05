using System.Collections.Generic;
using System.Threading.Tasks;
using Voguedi.AsyncExecution;

namespace Voguedi.Domain.Events
{
    public interface IEventStore
    {
        #region Methods

        Task<AsyncExecutedResult<EventStreamSavedResult>> SaveAsync(EventStream stream);

        Task<AsyncExecutedResult<EventStream>> GetByCommandIdAsync(string aggregateRootId, long commandId);

        Task<AsyncExecutedResult<EventStream>> GetByVersionAsync(string aggregateRootId, long version);

        Task<AsyncExecutedResult<IReadOnlyList<EventStream>>> GetAllAsync(
            string aggregateRootTypeName,
            string aggregateRootId,
            long minVersion = -1L,
            long maxVersion = long.MaxValue);

        #endregion
    }
}
