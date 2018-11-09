using System;
using Voguedi;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class VoguediOptionsExtensions
    {
        #region Public Methods

        public static VoguediOptions UseRabbitMQ(this VoguediOptions options, Action<RabbitMQOptions> setupAction)
        {
            if (setupAction == null)
                throw new ArgumentNullException(nameof(setupAction));

            options.Register(new RabbitMQServiceRegistrar(setupAction));
            return options;
        }

        public static VoguediOptions UseRabbitMQ(this VoguediOptions options, string hostName, string exchangeName)
        {
            if (string.IsNullOrWhiteSpace(hostName))
                throw new ArgumentNullException(nameof(hostName));

            if (string.IsNullOrWhiteSpace(exchangeName))
                throw new ArgumentNullException(nameof(exchangeName));

            return options.UseRabbitMQ(s =>
            {
                s.HostName = hostName;
                s.ExchangeName = exchangeName;
            });
        }

        #endregion
    }
}
