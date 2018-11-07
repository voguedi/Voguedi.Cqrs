using System;
using System.Threading.Tasks;
using Voguedi.AsyncExecution;
using Voguedi.Domain.AggregateRoots;

namespace Voguedi.Domain.Caching
{
    public interface ICache
    {
        #region Methods

        Task<AsyncExecutedResult<IEventSourcedAggregateRoot>> GetAsync(Type aggregateRootType, string aggregateRootId);

        Task<AsyncExecutedResult> SetAsync(IEventSourcedAggregateRoot aggregateRoot);

        #endregion
    }
}
