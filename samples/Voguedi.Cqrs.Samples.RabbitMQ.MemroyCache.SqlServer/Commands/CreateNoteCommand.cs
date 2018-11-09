using Voguedi.Commands;

namespace Voguedi.Cqrs.Samples.RabbitMQ.MemroyCache.SqlServer.Commands
{
    [CommandSubscriber("CommandTopic")]
    public class CreateNoteCommand : Command<string>
    {
        #region Public Properties

        public string Title { get; }

        public string Content { get; }

        #endregion

        #region Ctors

        public CreateNoteCommand(string aggregateRootId, string title, string content)
            : base(aggregateRootId)
        {
            Title = title;
            Content = content;
        }

        #endregion
    }
}
