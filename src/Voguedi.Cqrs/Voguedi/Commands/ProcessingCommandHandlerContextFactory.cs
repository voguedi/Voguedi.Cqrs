using Voguedi.Domain.Caching;
using Voguedi.Domain.Repositories;

namespace Voguedi.Commands
{
    class ProcessingCommandHandlerContextFactory : IProcessingCommandHandlerContextFactory
    {
        #region Private Fields

        readonly ICache cache;
        readonly IRepository repository;

        #endregion

        #region Ctors

        public ProcessingCommandHandlerContextFactory(ICache cache, IRepository repository)
        {
            this.cache = cache;
            this.repository = repository;
        }

        #endregion

        #region IProcessingCommandHandlerContextFactory

        public IProcessingCommandHandlerContext Create() => new ProcessingCommandHandlerContext(cache, repository);

        #endregion
    }
}
