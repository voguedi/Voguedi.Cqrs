using System.Threading.Tasks;
using Voguedi.AsyncExecution;
using Voguedi.Stores;

namespace Voguedi.Events
{
    public interface IEventVersionStore : IStore
    {
        #region Methods

        Task<AsyncExecutedResult> SaveAsync(string aggregateRootTypeName, string aggregateRootId, long version);

        Task<AsyncExecutedResult<long>> GetAsync(string aggregateRootTypeName, string aggregateRootId);

        #endregion
    }
}
