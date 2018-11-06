using System.Threading.Tasks;
using Voguedi.Domain.AggregateRoots;

namespace Voguedi.Domain.Snapshots
{
    public interface ISnapshot
    {
        #region Methods

        Task<TAggregateRoot> RestoreAsync<TAggregateRoot, TIdentity>(TIdentity id) where TAggregateRoot : class, IAggregateRoot<TIdentity>;

        #endregion
    }
}
