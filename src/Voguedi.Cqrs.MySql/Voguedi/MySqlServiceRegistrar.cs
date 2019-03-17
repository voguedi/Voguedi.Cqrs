using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Voguedi.Domain.Events;
using Voguedi.Domain.Events.MySql;

namespace Voguedi
{
    class MySqlServiceRegistrar : IServiceRegistrar
    {
        #region Private Fields

        readonly Action<MySqlOptions> setupAction;

        #endregion

        #region Ctors

        public MySqlServiceRegistrar(Action<MySqlOptions> setupAction) => this.setupAction = setupAction;

        #endregion

        #region IServiceRegistrar

        public void Register(IServiceCollection services)
        {
            var options = new MySqlOptions();
            setupAction?.Invoke(options);
            services.AddSingleton(options);
            services.TryAddSingleton<IEventStore, MySqlEventStore>();
            services.TryAddSingleton<IEventVersionStore, MySqlEventVersionStore>();
        }

        #endregion
    }
}
