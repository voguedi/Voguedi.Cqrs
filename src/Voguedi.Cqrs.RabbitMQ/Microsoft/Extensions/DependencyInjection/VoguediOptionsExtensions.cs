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

        public static VoguediOptions UseRabbitMQ(this VoguediOptions options, string hostName)
        {
            if (string.IsNullOrWhiteSpace(hostName))
                throw new ArgumentNullException(nameof(hostName));

            return options.UseRabbitMQ(s => s.HostName = hostName);
        }

        #endregion
    }
}
