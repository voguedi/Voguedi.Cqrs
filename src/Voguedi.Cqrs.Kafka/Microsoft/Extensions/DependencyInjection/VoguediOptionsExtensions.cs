using System;
using Voguedi;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class VoguediOptionsExtensions
    {
        #region Public Methods

        public static VoguediOptions UseKafka(this VoguediOptions options, Action<KafkaOptions> setupAction)
        {
            if (setupAction == null)
                throw new ArgumentNullException(nameof(setupAction));

            options.Register(new KafkaServiceRegistrar(setupAction));
            return options;
        }

        public static VoguediOptions UseRabbitMQ(this VoguediOptions options, string servers)
        {
            if (string.IsNullOrWhiteSpace(servers))
                throw new ArgumentNullException(nameof(servers));

            return options.UseKafka(s => s.Servers = servers);
        }

        #endregion
    }
}
