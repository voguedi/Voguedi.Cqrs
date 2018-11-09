using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Voguedi.Domain.Caching;
using Voguedi.Domain.Caching.MemoryCache;

namespace Voguedi
{
    class MemoryCacheServiceRegistrar : IServiceRegistrar
    {
        #region Private Fields

        readonly Action<MemoryCacheOptions> setupAction;

        #endregion

        #region Ctors

        public MemoryCacheServiceRegistrar(Action<MemoryCacheOptions> setupAction) => this.setupAction = setupAction;

        #endregion

        #region IServiceRegistrar

        public void Register(IServiceCollection services)
        {
            var options = new MemoryCacheOptions();
            setupAction?.Invoke(options);
            services.AddSingleton(options);
            services.AddMemoryCache();
            services.AddDistributedMemoryCache();
            services.TryAddSingleton<ICache, MemoryCache>();
        }

        #endregion
    }
}
