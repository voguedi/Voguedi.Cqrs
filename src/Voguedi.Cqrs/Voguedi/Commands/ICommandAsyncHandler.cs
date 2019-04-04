using System.Threading.Tasks;
using Voguedi.ApplicationMessages;
using Voguedi.Infrastructure;

namespace Voguedi.Commands
{
    public interface ICommandAsyncHandler { }

    public interface ICommandAsyncHandler<in TCommand> : ICommandAsyncHandler
        where TCommand : class, ICommand
    {
        #region Methods

        Task<AsyncExecutedResult<IApplicationMessage>> HandleAsync(TCommand command);

        #endregion
    }
}
