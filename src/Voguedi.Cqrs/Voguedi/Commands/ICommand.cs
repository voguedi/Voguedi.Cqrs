using Voguedi.Messaging;

namespace Voguedi.Commands
{
    public interface ICommand : IMessage
    {
        #region Methods

        string GetAggregateRootId();

        #endregion
    }
}
