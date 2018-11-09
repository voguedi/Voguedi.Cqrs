using System.Threading.Tasks;

namespace Voguedi.Domain.Events
{
    public interface ICommittingEventHandler
    {
        #region Methods

        Task HandleAsync(CommittingEvent committingEvent);

        #endregion
    }
}
