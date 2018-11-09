using System;
using Voguedi;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class VoguediOptionsExtensions
    {
        #region Public Methods

        public static VoguediOptions UseMemoryCache(this VoguediOptions options, Action<MemoryCacheOptions> setupAction)
        {
            if (setupAction == null)
                throw new ArgumentNullException(nameof(setupAction));

            options.Register(new MemoryCacheServiceRegistrar(setupAction));
            return options;
        }

        public static VoguediOptions UseMemoryCache(this VoguediOptions options, TimeSpan slidingExpiration)
        {
            if (slidingExpiration < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(slidingExpiration));

            return options.UseMemoryCache(s => s.SlidingExpiration = slidingExpiration);
        }

        #endregion
    }
}
