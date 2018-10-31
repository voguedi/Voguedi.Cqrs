using System.Threading.Tasks;

namespace Voguedi.Commands
{
    public interface ICommandHandler { }

    public interface ICommandHandler<in TCommand> : ICommandHandler
        where TCommand : class, ICommand
    {
        #region Methods

        Task HandleAsync(ICommandHandlerContext context, TCommand command);

        #endregion
    }
}
