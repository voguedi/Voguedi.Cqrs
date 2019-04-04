using Voguedi.Messaging;

namespace Voguedi.Domain.Events
{
    public class EventSubscriberAttribute : MessageSubscriberAttribute
    {
        #region Ctors

        public EventSubscriberAttribute(string topic) : base(topic) { }

        #endregion
    }
}
