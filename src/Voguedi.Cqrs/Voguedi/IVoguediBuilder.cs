using Microsoft.Extensions.DependencyInjection;

namespace Voguedi
{
    public interface IVoguediBuilder
    {
        #region Properties

        IServiceCollection Services { get; }

        #endregion
    }
}
