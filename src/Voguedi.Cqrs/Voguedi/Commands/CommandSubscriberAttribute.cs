using Voguedi.Messaging;

namespace Voguedi.Commands
{
    public class CommandSubscriberAttribute : MessageSubscriberAttribute
    {
        #region Ctors

        public CommandSubscriberAttribute(string topic) : base(topic) { }

        #endregion
    }
}
