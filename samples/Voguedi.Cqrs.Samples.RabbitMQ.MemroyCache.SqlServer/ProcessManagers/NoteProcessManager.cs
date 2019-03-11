using System.Threading.Tasks;
using Voguedi.ApplicationMessages;
using Voguedi.AsyncExecution;
using Voguedi.Commands;
using Voguedi.Cqrs.Samples.RabbitMQ.MemroyCache.SqlServer.ApplicationMessages;
using Voguedi.Cqrs.Samples.RabbitMQ.MemroyCache.SqlServer.Commands;

namespace Voguedi.Cqrs.Samples.RabbitMQ.MemroyCache.SqlServer.ProcessManagers
{
    public class NoteProcessManager : IApplicationMessageHandler<ModifyNoteApplicationMessage>
    {
        #region Private Fields

        readonly ICommandSender commandSender;

        #endregion

        #region Ctors

        public NoteProcessManager(ICommandSender commandSender) => this.commandSender = commandSender;

        #endregion

        #region IApplicationMessageHandler<ModifyNoteApplicationMessage>

        public Task<AsyncExecutedResult> HandleAsync(ModifyNoteApplicationMessage message)
            => commandSender.SendAsync(new ModifyNoteCommand(message.NoteId, message.Title, message.Content) { Id = message.Id });

        #endregion
    }
}
