using Voguedi.Messaging;

namespace Voguedi.ApplicationMessages
{
    public interface IApplicationMessageHandler : IMessageHandler { }

    public interface IApplicationMessageHandler<in TApplicationMessage> : IApplicationMessageHandler, IMessageHandler<TApplicationMessage>
        where TApplicationMessage : class, IApplicationMessage { }
}
