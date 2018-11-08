using System.Threading.Tasks;

namespace Voguedi
{
    public interface IBootstrapper
    {
        #region Methods

        Task BootstrapperAsync();

        #endregion
    }
}
