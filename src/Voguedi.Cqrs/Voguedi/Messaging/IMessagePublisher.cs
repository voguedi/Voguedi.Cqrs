using System.Threading.Tasks;
using Voguedi.AsyncExecution;

namespace Voguedi.Messaging
{
    public interface IMessagePublisher
    {
        #region Methods

        Task<AsyncExecutedResult> PublishAsync(IMessage message);

        #endregion
    }
}
