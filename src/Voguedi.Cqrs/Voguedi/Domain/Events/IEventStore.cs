using System.Collections.Generic;
using System.Threading.Tasks;
using Voguedi.AspectCore;
using Voguedi.AsyncExecution;

namespace Voguedi.Domain.Events
{
    public interface IEventStore
    {
        #region Methods

        Task<AsyncExecutedResult<EventStreamSavedResult>> SaveAsync([NotNull] EventStream stream);

        Task<AsyncExecutedResult<EventStream>> GetByCommandIdAsync([NotEmpty] string aggregateRootId, long commandId);

        Task<AsyncExecutedResult<EventStream>> GetByVersionAsync([NotEmpty] string aggregateRootId, long version);

        Task<AsyncExecutedResult<IReadOnlyList<EventStream>>> GetAllAsync(
            [NotEmpty] string aggregateRootTypeName,
            [NotEmpty] string aggregateRootId,
            long minVersion = -1L,
            long maxVersion = long.MaxValue);

        #endregion
    }
}
