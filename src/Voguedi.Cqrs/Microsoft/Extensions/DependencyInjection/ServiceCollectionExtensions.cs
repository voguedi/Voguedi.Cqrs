using System;
using Voguedi;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        #region Public Methods

        public static IServiceCollection AddVoguedi(this IServiceCollection services, Action<VoguediOptions> setupAction)
        {
            if (setupAction == null)
                throw new ArgumentNullException(nameof(setupAction));

            services.AddDependencyServices();
            var options = new VoguediOptions();
            setupAction(options);
            services.AddSingleton(options);
            return services;
        }

        #endregion
    }
}
