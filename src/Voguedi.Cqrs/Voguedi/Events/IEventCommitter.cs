using System.Threading.Tasks;

namespace Voguedi.Events
{
    public interface IEventCommitter
    {
        #region Methods

        Task CommitAsync(CommittingEvent committingEvent);

        #endregion
    }
}
