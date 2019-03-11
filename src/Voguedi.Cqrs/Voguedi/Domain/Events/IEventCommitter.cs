using System.Threading.Tasks;
using Voguedi.Services;

namespace Voguedi.Domain.Events
{
    public interface IEventCommitter : IBackgroundWorkerService
    {
        #region Methods

        Task CommitAsync(CommittingEvent committingEvent);

        #endregion
    }
}
