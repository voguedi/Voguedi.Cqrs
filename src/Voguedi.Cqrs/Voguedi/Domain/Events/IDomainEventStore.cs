using System.Threading.Tasks;
using Voguedi.AsyncExecution;

namespace Voguedi.Domain.Events
{
    public interface IDomainEventStore
    {
        #region Methods

        Task<AsyncExecutionResult<DomainEventStreamSavedResult>> SaveStreamAsync(DomainEventStream stream);

        Task<AsyncExecutionResult<DomainEventStream>> GetStreamAsync(string aggregateRootId, string commandId);

        Task<AsyncExecutionResult<DomainEventStream>> GetStreamAsync(string aggregateRootId, long version);

        #endregion
    }
}
