using System.Threading.Tasks;

namespace Voguedi.Domain.Events
{
    public interface IDomainEventHandler { }

    public interface IDomainEventHandler<in TDomainEvent> : IDomainEventHandler
        where TDomainEvent : class, IDomainEvent
    {
        #region Methods

        Task HandleAsync(TDomainEvent e);

        #endregion
    }
}
