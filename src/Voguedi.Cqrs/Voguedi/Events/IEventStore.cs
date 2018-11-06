using System.Collections.Generic;
using System.Threading.Tasks;
using Voguedi.AsyncExecution;

namespace Voguedi.Events
{
    public interface IEventStore
    {
        #region Methods

        Task<AsyncExecutedResult<EventStreamSavedResult>> SaveStreamAsync(EventStream stream);

        Task<AsyncExecutedResult<EventStream>> GetStreamAsync(string aggregateRootId, string commandId);

        Task<AsyncExecutedResult<EventStream>> GetStreamAsync(string aggregateRootId, long version);

        Task<AsyncExecutedResult<IReadOnlyList<EventStream>>> GetStreamsAsync(
            string aggregateRootTypeName,
            string aggregateRootId,
            long minVersion = long.MinValue,
            long maxVersion = long.MaxValue);

        #endregion
    }
}
