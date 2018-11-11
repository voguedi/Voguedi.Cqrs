using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Voguedi.Domain.Events;
using Voguedi.Domain.Events.MongoDB;
using Voguedi.MongoDB;

namespace Voguedi
{
    class MongoDBServiceRegistrar<TDbContext> : IServiceRegistrar
        where TDbContext : class, IMongoDBContext
    {
        #region Private Fields

        readonly Action<MongoDBOptions> setupAction;

        #endregion

        #region Ctors

        public MongoDBServiceRegistrar(Action<MongoDBOptions> setupAction) => this.setupAction = setupAction;

        #endregion

        #region IServiceRegistrar

        public void Register(IServiceCollection services)
        {
            services.AddMongoDB<TDbContext>(setupAction);
            services.TryAddSingleton<IEventStore, MongoDBEventStore>();
            services.TryAddSingleton<IEventVersionStore, MongoDBEventVersionStore>();
        }

        #endregion
    }
}
