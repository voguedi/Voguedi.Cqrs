using Voguedi.Domain.Entities;

namespace Voguedi.Domain.AggregateRoots
{
    public abstract class AggregateRoot<TIdentity> : Entity<TIdentity>, IAggregateRoot<TIdentity>
    {
        #region Ctors

        protected AggregateRoot(TIdentity id) : base(id) { }

        #endregion
    }
}
