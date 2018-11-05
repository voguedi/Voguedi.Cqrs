using System.Threading.Tasks;

namespace Voguedi.Events
{
    public interface ICommittingEventHandler
    {
        #region Methods

        Task HandleAsync(CommittingEvent committingEvent);

        #endregion
    }
}
