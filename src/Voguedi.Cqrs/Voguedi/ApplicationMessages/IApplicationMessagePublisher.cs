using System.Threading.Tasks;
using Voguedi.Infrastructure;

namespace Voguedi.ApplicationMessages
{
    public interface IApplicationMessagePublisher
    {
        #region Methods

        Task<AsyncExecutedResult> PublishAsync(IApplicationMessage applicationMessage);

        #endregion
    }
}
