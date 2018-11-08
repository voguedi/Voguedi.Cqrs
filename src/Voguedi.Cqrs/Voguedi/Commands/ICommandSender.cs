using System.Threading.Tasks;
using Voguedi.AsyncExecution;

namespace Voguedi.Commands
{
    public interface ICommandSender
    {
        #region Methods

        Task<AsyncExecutedResult> SendAsync<TCommand>(TCommand command)
            where TCommand : class, ICommand;

        #endregion
    }
}
