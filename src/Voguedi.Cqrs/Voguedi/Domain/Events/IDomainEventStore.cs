using System.Collections.Generic;
using System.Threading.Tasks;
using Voguedi.AsyncExecution;

namespace Voguedi.Domain.Events
{
    public interface IDomainEventStore
    {
        #region Methods

        Task<AsyncExecutionResult<DomainEventStream>> GetStreamAsync(string aggregateRootId, string commandId);

        #endregion
    }
}
