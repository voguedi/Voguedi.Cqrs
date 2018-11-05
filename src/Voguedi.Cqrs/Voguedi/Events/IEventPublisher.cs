using System.Threading.Tasks;
using Voguedi.AsyncExecution;

namespace Voguedi.Events
{
    public interface IEventPublisher
    {
        #region Methods

        Task<AsyncExecutedResult> PublishStreamAsync(EventStream stream);

        #endregion
    }
}
