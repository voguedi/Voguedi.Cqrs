using System.Threading.Tasks;
using Voguedi.Messaging;

namespace Voguedi.Domain.Events
{
    public interface IEventCommitter : IMessageService
    {
        #region Methods

        Task CommitAsync(CommittingEvent committingEvent);

        #endregion
    }
}
