using Voguedi.Messaging;

namespace Voguedi.Domain.Events
{
    public interface IEventHandler : IMessageHandler { }

    public interface IEventHandler<in TEvent> : IEventHandler, IMessageHandler<TEvent> where TEvent : class, IEvent { }
}
