using Voguedi.Messaging;

namespace Voguedi.Commands
{
    public interface ICommand : IMessage
    {
        #region Properties

        string AggregateRootId { get; }

        #endregion
    }

    public interface ICommand<TIdentity> : ICommand
    {
        #region Properties

        new TIdentity AggregateRootId { get; set; }

        #endregion
    }
}
