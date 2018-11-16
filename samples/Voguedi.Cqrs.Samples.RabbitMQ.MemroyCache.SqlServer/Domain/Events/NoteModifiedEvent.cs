using Voguedi.Domain.Events;

namespace Voguedi.Cqrs.Samples.RabbitMQ.MemroyCache.SqlServer.Domain.Events
{
    [EventSubscriber("note")]
    public class NoteModifiedEvent : Event<string>
    {
        #region Public Properties

        public string Title { get; }

        public string Content { get; }

        #endregion

        #region Ctors

        public NoteModifiedEvent(string title, string content)
        {
            Title = title;
            Content = content;
        }

        #endregion
    }
}
