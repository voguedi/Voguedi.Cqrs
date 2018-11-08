using System.Threading;
using System.Threading.Tasks;

namespace Voguedi.Messaging
{
    public interface IMessageStore
    {
        #region Methods

        Task InitializeAsync(CancellationToken cancellationToken);

        #endregion
    }
}
