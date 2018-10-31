using System.Threading.Tasks;
using Voguedi.AsyncExecution;
using Voguedi.Commands;

namespace Voguedi.Domain.Events
{
    public interface IDomainEventPublisher
    {
        #region Methods

        Task<AsyncExecutionResult> PublisherAsync(DomainEventStream stream);

        #endregion
    }
}
