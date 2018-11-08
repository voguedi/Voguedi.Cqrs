using System.Threading.Tasks;
using Voguedi.Messaging;

namespace Voguedi.Events
{
    public interface IEventCommitter : IMessageService
    {
        #region Methods

        Task CommitAsync(CommittingEvent committingEvent);

        #endregion
    }
}
