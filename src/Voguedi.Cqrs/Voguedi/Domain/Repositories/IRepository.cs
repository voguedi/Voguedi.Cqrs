using System.Threading.Tasks;
using Voguedi.Domain.AggregateRoots;

namespace Voguedi.Domain.Repositories
{
    public interface IRepository
    {
        #region Methods

        Task<TAggregateRoot> Get<TAggregateRoot, TIdentity>(TIdentity id) where TAggregateRoot : class, IAggregateRoot<TIdentity>;

        #endregion
    }
}
