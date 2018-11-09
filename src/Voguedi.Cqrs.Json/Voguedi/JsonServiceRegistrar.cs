using Microsoft.Extensions.DependencyInjection;

namespace Voguedi
{
    class JsonServiceRegistrar : IServiceRegistrar
    {
        #region IServiceRegistrar

        public void Register(IServiceCollection services) => services.AddJson();

        #endregion
    }
}
