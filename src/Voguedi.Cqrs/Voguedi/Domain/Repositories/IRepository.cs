using System;
using System.Threading.Tasks;
using Voguedi.Domain.AggregateRoots;

namespace Voguedi.Domain.Repositories
{
    public interface IRepository
    {
        #region Methods

        Task<IEventSourcedAggregateRoot> GetAsync(Type aggregateRootType, string aggregateRootId);

        #endregion
    }
}
