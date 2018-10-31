using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Voguedi.Domain.Entities;
using Voguedi.Domain.Events;

namespace Voguedi.Domain.AggregateRoots
{
    public abstract class AggregateRoot<TIdentity> : Entity<TIdentity>, IAggregateRoot<TIdentity>
    {
        #region Ctors

        protected AggregateRoot(TIdentity id) : base(id) { }

        #endregion
    }
}
