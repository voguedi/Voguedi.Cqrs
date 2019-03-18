using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;
using Voguedi.Domain.Events;
using Voguedi.Domain.Events.MongoDB;
using Voguedi.Services;

namespace Voguedi
{
    class MongoDBServiceRegistrar : IServiceRegistrar
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
            var options = new MongoDBOptions();
            setupAction?.Invoke(options);
            services.AddSingleton(options);
            services.TryAddSingleton<IMongoClient>(new MongoClient(options.ConnectionString));
            services.TryAddSingleton<IEventStore, MongoDBEventStore>();
            services.TryAddSingleton<IEventVersionStore, MongoDBEventVersionStore>();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IStoreService, MongoDBEventStore>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IStoreService, MongoDBEventVersionStore>());
        }

        #endregion
    }
}
