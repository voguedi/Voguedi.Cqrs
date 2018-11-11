using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Voguedi.Domain.Events;
using Voguedi.Domain.Events.PostgreSql;
using Voguedi.Messaging;

namespace Voguedi
{
    class PostgreSqlServiceRegistrar : IServiceRegistrar
    {
        #region Private Fields

        readonly Action<PostgreSqlOptions> setupAction;

        #endregion

        #region Ctors

        public PostgreSqlServiceRegistrar(Action<PostgreSqlOptions> setupAction) => this.setupAction = setupAction;

        #endregion

        #region IServiceRegistrar

        public void Register(IServiceCollection services)
        {
            var options = new PostgreSqlOptions();
            setupAction?.Invoke(options);
            services.AddSingleton(options);
            services.TryAddSingleton<IEventStore, PostgreSqlEventStore>();
            services.TryAddSingleton<IEventVersionStore, PostgreSqlEventVersionStore>();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IMessageStore, PostgreSqlEventStore>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IMessageStore, PostgreSqlEventVersionStore>());
        }

        #endregion
    }
}
