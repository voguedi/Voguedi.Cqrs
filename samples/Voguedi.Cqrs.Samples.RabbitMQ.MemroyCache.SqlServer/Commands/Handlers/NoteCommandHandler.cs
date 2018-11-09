using System.Threading.Tasks;
using Voguedi.Commands;
using Voguedi.Cqrs.Samples.RabbitMQ.MemroyCache.SqlServer.Domain.Model;

namespace Voguedi.Cqrs.Samples.RabbitMQ.MemroyCache.SqlServer.Commands.Handlers
{
    class NoteCommandHandler
        : ICommandHandler<CreateNoteCommand>
        , ICommandHandler<ModifyNoteCommand>
    {
        #region ICommandHandler<CreateNoteCommand>

        public Task HandleAsync(ICommandHandlerContext context, CreateNoteCommand command)
            => context.CreateAggregateRootAsync<Note, string>(new Note(command.AggregateRootId, command.Title, command.Content));

        #endregion

        #region ICommandHandler<ModifyNoteCommand>

        public async Task HandleAsync(ICommandHandlerContext context, ModifyNoteCommand command)
        {
            var note = await context.GetAggregateRootAsync<Note, string>(command.AggregateRootId);
            note.Modify(command.Title, command.Content);
        }

        #endregion
    }
}
