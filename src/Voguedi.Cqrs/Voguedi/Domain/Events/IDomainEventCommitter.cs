using System.Threading.Tasks;
using Voguedi.AsyncExecution;

namespace Voguedi.Domain.Events
{
    public interface IDomainEventCommitter
    {
        #region Methods

        Task<AsyncExecutionResult> CommitAsync(CommittingDomainEvent committingEvent);

        #endregion
    }
}
