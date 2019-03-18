using System.Threading;
using System.Threading.Tasks;

namespace Voguedi.Services
{
    public interface IStoreService
    {
        #region Methods

        Task InitializeAsync(CancellationToken cancellationToken);

        #endregion
    }
}
