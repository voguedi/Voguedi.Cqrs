using Voguedi;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class VoguediOptionsExtensions
    {
        #region Public Methods

        public static VoguediOptions UseJson(this VoguediOptions options)
        {
            options.Register(new JsonServiceRegistrar());
            return options;
        }

        #endregion
    }
}
