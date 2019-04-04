using System.Threading.Tasks;
using Voguedi.Infrastructure;
using Voguedi.Services;

namespace Voguedi.Domain.Events
{
    public interface IEventVersionStore : IStoreService
    {
        #region Methods

        Task<AsyncExecutedResult> SaveAsync(string aggregateRootTypeName, string aggregateRootId, long version);

        Task<AsyncExecutedResult<long>> GetAsync(string aggregateRootTypeName, string aggregateRootId);

        #endregion
    }
}
