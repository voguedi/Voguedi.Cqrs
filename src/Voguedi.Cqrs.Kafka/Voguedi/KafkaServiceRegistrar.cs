using System;
using Microsoft.Extensions.DependencyInjection;

namespace Voguedi
{
    class KafkaServiceRegistrar : IServiceRegistrar
    {
        #region Private Fields

        readonly Action<KafkaOptions> setupAction;

        #endregion

        #region Ctors

        public KafkaServiceRegistrar(Action<KafkaOptions> setupAction) => this.setupAction = setupAction;

        #endregion

        #region IServiceRegistrar

        public void Register(IServiceCollection services) => services.AddKafka(setupAction);

        #endregion
    }
}
