using System.Threading.Tasks;
using Voguedi.Infrastructure;

namespace Voguedi.Domain.Events
{
    public interface IEventPublisher
    {
        #region Methods

        Task<AsyncExecutedResult> PublishStreamAsync(EventStream stream);

        #endregion
    }
}
