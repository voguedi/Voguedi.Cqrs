using System;
using Voguedi;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class VoguediOptionsExtensions
    {
        #region Public Methods

        public static VoguediOptions UseSqlServer(this VoguediOptions options, Action<SqlServerOptions> setupAction)
        {
            if (setupAction == null)
                throw new ArgumentNullException(nameof(setupAction));

            options.Register(new SqlServerServiceRegistrar(setupAction));
            return options;
        }

        public static VoguediOptions UseSqlServer(this VoguediOptions options, string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException(nameof(connectionString));

            return options.UseSqlServer(s => s.ConnectionString = connectionString);
        }

        #endregion
    }
}
