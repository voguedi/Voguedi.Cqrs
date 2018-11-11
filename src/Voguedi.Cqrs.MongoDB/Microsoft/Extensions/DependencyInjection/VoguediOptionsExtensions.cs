using System;
using Voguedi;
using Voguedi.MongoDB;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class VoguediOptionsExtensions
    {
        #region Public Methods

        public static VoguediOptions UseMongoDB<TDbContext>(this VoguediOptions options, Action<MongoDBOptions> setupAction)
            where TDbContext : class, IMongoDBContext
        {
            if (setupAction == null)
                throw new ArgumentNullException(nameof(setupAction));

            options.Register(new MongoDBServiceRegistrar<TDbContext>(setupAction));
            return options;
        }

        public static VoguediOptions UseMongoDB(this VoguediOptions options, Action<MongoDBOptions> setupAction) => options.UseMongoDB<MongoDBContext>(setupAction);

        #endregion
    }
}
