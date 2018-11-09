using Voguedi.Messaging;

namespace Voguedi.Domain.Events
{
    public sealed class EventSubscriberAttribute : MessageSubscriberAttribute
    {
        #region Ctors

        public EventSubscriberAttribute(string topic) : base(topic) { }

        #endregion
    }
}
