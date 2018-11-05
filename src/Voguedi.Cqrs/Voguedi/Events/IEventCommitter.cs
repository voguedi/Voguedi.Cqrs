using System.Threading.Tasks;
using Voguedi.Processors;

namespace Voguedi.Events
{
    public interface IEventCommitter : IProcessor
    {
        #region Methods

        Task CommitAsync(CommittingEvent committingEvent);

        #endregion
    }
}
