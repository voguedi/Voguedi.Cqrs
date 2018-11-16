using System;
using Voguedi.Messaging;

namespace Voguedi.Commands
{
    public abstract class Command<TIdentity> : Message, ICommand<TIdentity>
    {
        #region Ctors

        protected Command(TIdentity aggregateRootId)
            : base()
        {
            if (Equals(aggregateRootId, default(TIdentity)))
                throw new ArgumentNullException(nameof(aggregateRootId));

            AggregateRootId = aggregateRootId;
        }

        #endregion

        #region Message

        public override string GetRoutingKey() => AggregateRootId?.ToString();

        #endregion

        #region ICommand<TIdentity>

        public TIdentity AggregateRootId { get; protected set; }

        string ICommand.AggregateRootId => AggregateRootId?.ToString();

        #endregion
    }
}
