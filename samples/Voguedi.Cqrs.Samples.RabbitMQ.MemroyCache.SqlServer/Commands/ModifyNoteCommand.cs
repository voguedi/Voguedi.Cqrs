using Voguedi.Commands;

namespace Voguedi.Cqrs.Samples.RabbitMQ.MemroyCache.SqlServer.Commands
{
    [CommandSubscriber("note")]
    public class ModifyNoteCommand : Command<string>
    {
        #region Public Properties

        public string Title { get; }

        public string Content { get; }

        #endregion

        #region Ctors

        public ModifyNoteCommand(string aggregateRootId, string title, string content)
            : base(aggregateRootId)
        {
            Title = title;
            Content = content;
        }

        #endregion
    }
}
