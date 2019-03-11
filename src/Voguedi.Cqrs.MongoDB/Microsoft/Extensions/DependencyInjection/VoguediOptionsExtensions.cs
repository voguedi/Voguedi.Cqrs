using System;
using Voguedi;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class VoguediOptionsExtensions
    {
        #region Public Methods
        
        public static VoguediOptions UseMongoDB(this VoguediOptions options, Action<MongoDBOptions> setupAction)
        {
            if (setupAction == null)
                throw new ArgumentNullException(nameof(setupAction));

            options.Register(new MongoDBServiceRegistrar(setupAction));
            return options;
        }

        #endregion
    }
}
