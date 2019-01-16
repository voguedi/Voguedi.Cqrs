using System.Threading.Tasks;
using Voguedi.AsyncExecution;
using Voguedi.AspectCore;

namespace Voguedi.Domain.Events
{
    public interface IEventVersionStore
    {
        #region Methods

        Task<AsyncExecutedResult> SaveAsync([NotEmpty] string aggregateRootTypeName, [NotEmpty] string aggregateRootId, long version);

        Task<AsyncExecutedResult<long>> GetAsync([NotEmpty] string aggregateRootTypeName, [NotEmpty] string aggregateRootId);

        #endregion
    }
}
