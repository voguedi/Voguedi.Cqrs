using System.Threading;
using System.Threading.Tasks;

namespace Voguedi.Stores
{
    public interface IStore
    {
        #region Methods

        Task InitializeAsync(CancellationToken cancellationToken);

        #endregion
    }
}
