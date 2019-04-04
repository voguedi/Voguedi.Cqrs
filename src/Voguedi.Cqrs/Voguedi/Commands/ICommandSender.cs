using System.Threading.Tasks;
using Voguedi.Infrastructure;

namespace Voguedi.Commands
{
    public interface ICommandSender
    {
        #region Methods

        Task<AsyncExecutedResult> SendAsync<TCommand>(TCommand command) where TCommand : class, ICommand;

        #endregion
    }
}
