using Voguedi.Domain.Events;

namespace Voguedi.Cqrs.Samples.RabbitMQ.MemroyCache.SqlServer.Domain.Events
{
    [EventSubscriber("EventTopic")]
    public class NoteCreatedEvent : Event<string>
    {
        #region Public Properties

        public string Title { get; }

        public string Content { get; }

        #endregion

        #region Ctors
        
        public NoteCreatedEvent(string title, string content)
        {
            Title = title;
            Content = content;
        }

        #endregion
    }
}
