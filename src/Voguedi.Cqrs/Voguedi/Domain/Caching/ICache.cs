using System;
using System.Threading.Tasks;
using Voguedi.Domain.AggregateRoots;

namespace Voguedi.Domain.Caching
{
    public interface ICache
    {
        #region Methods

        Task<IEventSourcedAggregateRoot> GetAsync(Type aggregateRootType, string aggregateRootId);

        Task SetAsync(IEventSourcedAggregateRoot aggregateRoot);

        Task RefreshAsync(Type aggregateRootType, string aggregateRootId);

        #endregion
    }
}
