using System;
using System.Threading.Tasks;
using Voguedi.Domain.AggregateRoots;

namespace Voguedi.Domain.Caching
{
    public interface ICache : IDisposable
    {
        #region Methods

        Task<TAggregateRoot> GetAsync<TAggregateRoot, TIdentity>(TIdentity aggregateRootId) where TAggregateRoot : class, IAggregateRoot<TIdentity>;

        Task SetAsync(IAggregateRoot aggregateRoot);

        Task RefreshAsync(Type aggregateRootType, string aggregateRootId);

        void Start();

        #endregion
    }
}
