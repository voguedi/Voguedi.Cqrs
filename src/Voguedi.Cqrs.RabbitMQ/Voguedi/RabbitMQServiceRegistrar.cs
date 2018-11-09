using System;
using Microsoft.Extensions.DependencyInjection;

namespace Voguedi
{
    class RabbitMQServiceRegistrar : IServiceRegistrar
    {
        #region Private Fields

        readonly Action<RabbitMQOptions> setupAction;

        #endregion

        #region Ctors

        public RabbitMQServiceRegistrar(Action<RabbitMQOptions> setupAction) => this.setupAction = setupAction;

        #endregion

        #region IServiceRegistrar

        public void Register(IServiceCollection services) => services.AddRabbitMQ(setupAction);

        #endregion
    }
}
