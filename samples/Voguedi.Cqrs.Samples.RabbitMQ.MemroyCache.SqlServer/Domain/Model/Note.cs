using Voguedi.Cqrs.Samples.RabbitMQ.MemroyCache.SqlServer.Domain.Events;
using Voguedi.Domain.AggregateRoots;

namespace Voguedi.Cqrs.Samples.RabbitMQ.MemroyCache.SqlServer.Domain.Model
{
    public class Note : AggregateRoot<string>
    {
        #region Public Properties

        public string Title { get; private set; }

        public string Content { get; private set; }

        #endregion

        #region Ctors

        public Note() : base() { }

        public Note(string id) : base(id) { }

        public Note(string id, string title, string content) : this(id) => ApplyEvent(new NoteCreatedEvent(title, content));

        #endregion

        #region Private Methods

        void Handle(NoteCreatedEvent e)
        {
            Title = e.Title;
            Content = e.Content;
        }

        void Handle(NoteModifiedEvent e)
        {
            Title = e.Title;
            Content = e.Content;
        }

        #endregion

        #region Public Methods

        public void Modify(string title, string content) => ApplyEvent(new NoteModifiedEvent(title, content));

        #endregion
    }
}
