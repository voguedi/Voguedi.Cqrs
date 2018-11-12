using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Voguedi.Domain.Events;
using Voguedi.Domain.Events.SqlServer;
using Voguedi.Stores;

namespace Voguedi
{
    class SqlServerServiceRegistrar : IServiceRegistrar
    {
        #region Private Fields

        readonly Action<SqlServerOptions> setupAction;

        #endregion

        #region Ctors

        public SqlServerServiceRegistrar(Action<SqlServerOptions> setupAction) => this.setupAction = setupAction;

        #endregion

        #region IServiceRegistrar

        public void Register(IServiceCollection services)
        {
            var options = new SqlServerOptions();
            setupAction?.Invoke(options);
            services.AddSingleton(options);
            services.TryAddSingleton<IEventStore, SqlServerEventStore>();
            services.TryAddSingleton<IEventVersionStore, SqlServerEventVersionStore>();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IStore, SqlServerEventStore>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IStore, SqlServerEventVersionStore>());
        }

        #endregion
    }
}
