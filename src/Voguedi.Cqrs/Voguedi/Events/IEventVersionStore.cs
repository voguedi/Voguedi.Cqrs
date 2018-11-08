using System.Threading;
using System.Threading.Tasks;
using Voguedi.AsyncExecution;

namespace Voguedi.Events
{
    public interface IEventVersionStore
    {
        #region Methods

        Task<AsyncExecutedResult> SaveAsync(string aggregateRootTypeName, string aggregateRootId, long version);

        Task<AsyncExecutedResult<long>> GetAsync(string aggregateRootTypeName, string aggregateRootId);

        Task InitializeAsync(CancellationToken cancellationToken);

        #endregion
    }
}
