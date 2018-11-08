using Microsoft.Extensions.DependencyInjection;
using Voguedi;

namespace Microsoft.AspNetCore.Builder
{
    public static class ApplicationBuilderExtensions
    {
        #region Public Methods

        public static IApplicationBuilder UseVoguedi(IApplicationBuilder applicationBuilder)
        {
            applicationBuilder.ApplicationServices.GetRequiredService<IBootstrapper>().BootstrapperAsync();
            return applicationBuilder;
        }

        #endregion
    }
}
