using System.Threading.Tasks;
using Voguedi.AsyncExecution;

namespace Voguedi.Domain.Events
{
    public interface IDomainEventPublishedVersionStore
    {
        #region Methods

        Task<AsyncExecutionResult> SaveAsync(string aggregateRootTypeName, string aggregateRootId, long version);

        Task<AsyncExecutionResult<long>> GetAsync(string aggregateRootTypeName, string aggregateRootId);

        #endregion
    }
}
