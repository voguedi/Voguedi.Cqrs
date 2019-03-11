using Voguedi.ApplicationMessages;

namespace Voguedi.Cqrs.Samples.RabbitMQ.MemroyCache.SqlServer.ApplicationMessages
{
    [ApplicationMessageSubscriber("note")]
    public class ModifyNoteApplicationMessage : ApplicationMessage
    {
        #region Ctors

        public ModifyNoteApplicationMessage() { }

        public ModifyNoteApplicationMessage(string noteId, string title, string content)
        {
            NoteId = noteId;
            Title = title;
            Content = content;
        }

        #endregion

        #region Public Properties

        public string NoteId { get; set; }

        public string Title { get; set; }

        public string Content { get; set; }

        #endregion

        #region ApplicationMessage

        public override string GetRoutingKey() => NoteId;

        #endregion
    }
}
