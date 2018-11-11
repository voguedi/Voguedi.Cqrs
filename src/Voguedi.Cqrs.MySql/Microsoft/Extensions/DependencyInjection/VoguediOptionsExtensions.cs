using System;
using Voguedi;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class VoguediOptionsExtensions
    {
        #region Public Methods

        public static VoguediOptions UseMySql(this VoguediOptions options, Action<MySqlOptions> setupAction)
        {
            if (setupAction == null)
                throw new ArgumentNullException(nameof(setupAction));

            options.Register(new MySqlServiceRegistrar(setupAction));
            return options;
        }

        public static VoguediOptions UseMySql(this VoguediOptions options, string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException(nameof(connectionString));

            return options.UseMySql(s => s.ConnectionString = connectionString);
        }

        #endregion
    }
}
