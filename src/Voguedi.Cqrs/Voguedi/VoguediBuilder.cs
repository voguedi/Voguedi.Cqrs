using Microsoft.Extensions.DependencyInjection;

namespace Voguedi
{
    class VoguediBuilder : IVoguediBuilder
    {
        #region Ctors

        public VoguediBuilder(IServiceCollection services) => Services = services;

        #endregion

        #region IVoguediBuilder

        public IServiceCollection Services { get; }

        #endregion
    }
}
