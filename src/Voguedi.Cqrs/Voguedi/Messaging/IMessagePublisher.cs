using System.Threading.Tasks;
using Voguedi.Infrastructure;

namespace Voguedi.Messaging
{
    public interface IMessagePublisher
    {
        #region Methods

        Task<AsyncExecutedResult> PublishAsync(IMessage message);

        #endregion
    }
}
