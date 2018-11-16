using Voguedi.Commands;

namespace Voguedi.Cqrs.Samples.RabbitMQ.MemroyCache.SqlServer.Commands
{
    [CommandSubscriber("note")]
    public class ModifyNoteCommand : Command<string>
    {
        #region Public Properties

        public string Title { get; set; }

        public string Content { get; set; }

        #endregion

        #region Ctors

        public ModifyNoteCommand() { }

        public ModifyNoteCommand(string noteId, string title, string content)
            : base(noteId)
        {
            Title = title;
            Content = content;
        }

        #endregion
    }
}
