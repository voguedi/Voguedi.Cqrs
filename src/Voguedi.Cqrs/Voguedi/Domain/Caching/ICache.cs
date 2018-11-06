using System.Threading.Tasks;
using Voguedi.Domain.AggregateRoots;

namespace Voguedi.Domain.Caching
{
    public interface ICache
    {
        #region Methods

        Task<TAggregateRoot> GetAsync<TAggregateRoot, TIdentity>(TIdentity id) where TAggregateRoot : class, IAggregateRoot<TIdentity>;

        #endregion
    }
}
