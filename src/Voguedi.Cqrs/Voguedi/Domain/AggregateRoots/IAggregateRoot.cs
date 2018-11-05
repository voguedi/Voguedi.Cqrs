using Voguedi.Domain.Entities;

namespace Voguedi.Domain.AggregateRoots
{
    public interface IAggregateRoot<TIdentity> : IEntity<TIdentity>, IEventSourcedAggregateRoot { }
}
