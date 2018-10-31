using System.Collections.Generic;
using Voguedi.Domain.AggregateRoots;

namespace Voguedi.Commands
{
    public interface IProcessingCommandHandlerContext : ICommandHandlerContext
    {
        #region Methods

        IReadOnlyList<IEventSourcedAggregateRoot> GetAggregateRoots();

        #endregion
    }
}
