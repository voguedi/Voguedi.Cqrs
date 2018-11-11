using System;
using Voguedi;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class VoguediOptionsExtensions
    {
        #region Public Methods

        public static VoguediOptions UsePostgreSql(this VoguediOptions options, Action<PostgreSqlOptions> setupAction)
        {
            if (setupAction == null)
                throw new ArgumentNullException(nameof(setupAction));

            options.Register(new PostgreSqlServiceRegistrar(setupAction));
            return options;
        }

        public static VoguediOptions UsePostgreSql(this VoguediOptions options, string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException(nameof(connectionString));

            return options.UsePostgreSql(s => s.ConnectionString = connectionString);
        }

        #endregion
    }
}
