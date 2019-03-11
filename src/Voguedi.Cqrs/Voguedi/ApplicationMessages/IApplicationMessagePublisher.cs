using System.Threading.Tasks;
using Voguedi.AsyncExecution;

namespace Voguedi.ApplicationMessages
{
    public interface IApplicationMessagePublisher
    {
        #region Methods

        Task<AsyncExecutedResult> PublishAsync(IApplicationMessage applicationMessage);

        #endregion
    }
}
