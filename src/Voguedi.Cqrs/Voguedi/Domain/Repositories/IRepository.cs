using System;
using System.Threading.Tasks;
using Voguedi.Domain.AggregateRoots;
using Voguedi.AsyncExecution;

namespace Voguedi.Domain.Repositories
{
    public interface IRepository
    {
        #region Methods

        Task<AsyncExecutedResult<TAggregateRoot>> GetAsync<TAggregateRoot, TIdentity>(TIdentity id) where TAggregateRoot : class, IAggregateRoot<TIdentity>;

        Task<AsyncExecutedResult<IEventSourcedAggregateRoot>> GetAsync(Type aggregateRootType, string aggregateRootId);

        #endregion
    }
}
