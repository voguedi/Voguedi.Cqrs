using System.Threading.Tasks;
using Voguedi.Services;

namespace Voguedi.Domain.Events
{
    public interface IEventCommitter : IService
    {
        #region Methods

        Task CommitAsync(CommittingEvent committingEvent);

        #endregion
    }
}
