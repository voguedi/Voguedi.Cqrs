using System.Threading.Tasks;
using Voguedi.Infrastructure;
using Voguedi.Messaging;

namespace Voguedi.Commands
{
    class CommandSender : ICommandSender
    {
        #region Private Fields

        readonly IMessagePublisher publisher;

        #endregion

        #region Ctors

        public CommandSender(IMessagePublisher publisher) => this.publisher = publisher;

        #endregion

        #region ICommandSender

        Task<AsyncExecutedResult> ICommandSender.SendAsync<TCommand>(TCommand command) => publisher.PublishAsync(command);

        #endregion
    }
}
