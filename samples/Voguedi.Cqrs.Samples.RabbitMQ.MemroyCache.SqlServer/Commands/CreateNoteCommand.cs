using Voguedi.Commands;

namespace Voguedi.Cqrs.Samples.RabbitMQ.MemroyCache.SqlServer.Commands
{
    [CommandSubscriber("note")]
    public class CreateNoteCommand : Command<string>
    {
        #region Ctors

        public CreateNoteCommand() { }

        public CreateNoteCommand(string noteId, string title, string content)
            : base(noteId)
        {
            Title = title;
            Content = content;
        }

        #endregion

        #region Public Properties

        public string Title { get; set; }

        public string Content { get; set; }

        #endregion
    }
}
