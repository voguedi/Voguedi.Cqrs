using System;
using System.Threading.Tasks;
using Voguedi.Domain.AggregateRoots;

namespace Voguedi.Domain.Repositories
{
    public interface IRepository
    {
        #region Methods

        Task<TAggregateRoot> GetAsync<TAggregateRoot, TIdentity>(TIdentity aggregateRootId) where TAggregateRoot : class, IAggregateRoot<TIdentity>;

        Task<IAggregateRoot> GetAsync(Type aggregateRootType, string aggregateRootId);

        #endregion
    }
}
