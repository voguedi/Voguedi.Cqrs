using System.Threading.Tasks;
using Voguedi.AsyncExecution;

namespace Voguedi.Domain.Events
{
    public interface IEventPublisher
    {
        #region Methods

        Task<AsyncExecutedResult> PublishStreamAsync(EventStream stream);

        #endregion
    }
}
