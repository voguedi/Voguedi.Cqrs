using Voguedi.Messaging;

namespace Voguedi.ApplicationMessages
{
    public sealed class ApplicationMessageSubscriberAttribute : MessageSubscriberAttribute
    {
        #region Ctors

        public ApplicationMessageSubscriberAttribute(string topic) : base(topic) { }

        #endregion
    }
}
