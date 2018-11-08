using Voguedi.Messaging;

namespace Voguedi.Commands
{
    public sealed class CommandSubscriberAttribute : MessageSubscriberAttribute
    {
        #region Ctors

        public CommandSubscriberAttribute(string topic) : base(topic) { }

        #endregion
    }
}
