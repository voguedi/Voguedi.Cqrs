using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Voguedi.Events;
using Voguedi.Events.SqlServer;
using Voguedi.Messaging;

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
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IMessageStore, SqlServerEventStore>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IMessageStore, SqlServerEventVersionStore>());
        }

        #endregion
    }
}
