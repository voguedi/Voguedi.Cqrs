using Microsoft.Extensions.DependencyInjection;

namespace Voguedi
{
    public interface IServiceRegistrar
    {
        #region Methods

        void Register(IServiceCollection services);

        #endregion
    }
}
