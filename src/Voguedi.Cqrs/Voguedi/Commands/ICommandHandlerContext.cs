using System.Threading.Tasks;
using Voguedi.Domain.AggregateRoots;

namespace Voguedi.Commands
{
    public interface ICommandHandlerContext
    {
        #region Methods

        Task CreateAggregateRoot<TAggregateRoot, TIdentity>(TAggregateRoot aggregateRoot) where TAggregateRoot : class, IAggregateRoot<TIdentity>;

        Task<TAggregateRoot> GetAggregateRoot<TAggregateRoot, TIdentity>(TIdentity aggregateRootId) where TAggregateRoot : class, IAggregateRoot<TIdentity>;

        #endregion
    }
}
