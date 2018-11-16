using Voguedi.Domain.Events;

namespace Voguedi.Cqrs.Samples.RabbitMQ.MemroyCache.SqlServer.Domain.Events
{
    [EventSubscriber("note")]
    public class NoteCreatedEvent : Event<string>
    {
        #region Public Properties

        public string Title { get; set; }

        public string Content { get; set; }

        #endregion

        #region Ctors

        public NoteCreatedEvent() { }

        public NoteCreatedEvent(string title, string content)
        {
            Title = title;
            Content = content;
        }

        #endregion
    }
}
