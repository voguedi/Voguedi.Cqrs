using Voguedi.Domain.Events;

namespace Voguedi.Cqrs.Samples.RabbitMQ.MemroyCache.SqlServer.Domain.Events
{
    [EventSubscriber("note")]
    public class NoteModifiedEvent : Event<string>
    {
        #region Public Properties

        public string Title { get; set; }

        public string Content { get; set; }

        #endregion

        #region Ctors

        public NoteModifiedEvent() { }

        public NoteModifiedEvent(string title, string content)
        {
            Title = title;
            Content = content;
        }

        #endregion
    }
}
