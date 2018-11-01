using System.Threading.Tasks;
using Voguedi.AsyncExecution;

namespace Voguedi.Domain.Events
{
    public interface IDomainEventPublisher
    {
        #region Methods

        Task<AsyncExecutionResult> PublisheAsync(DomainEventStream stream);

        #endregion
    }
}
