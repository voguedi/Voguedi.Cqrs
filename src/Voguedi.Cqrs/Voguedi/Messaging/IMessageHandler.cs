using System.Threading.Tasks;
using Voguedi.Infrastructure;

namespace Voguedi.Messaging
{
    public interface IMessageHandler { }

    public interface IMessageHandler<in TMessage> : IMessageHandler
        where TMessage : class, IMessage
    {
        #region Methods

        Task<AsyncExecutedResult> HandleAsync(TMessage message);

        #endregion
    }
}
