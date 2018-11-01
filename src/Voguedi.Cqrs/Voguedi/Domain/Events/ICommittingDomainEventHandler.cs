using System.Threading.Tasks;

namespace Voguedi.Domain.Events
{
    public interface ICommittingDomainEventHandler
    {
        #region Methods

        Task HandleAsync(CommittingDomainEvent committingEvent);

        #endregion
    }
}
