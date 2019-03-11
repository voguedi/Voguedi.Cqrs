using System;
using System.Threading.Tasks;
using Voguedi.ApplicationMessages;
using Voguedi.AsyncExecution;
using Voguedi.Commands;
using Voguedi.Cqrs.Samples.RabbitMQ.MemroyCache.SqlServer.ApplicationMessages;
using Voguedi.Cqrs.Samples.RabbitMQ.MemroyCache.SqlServer.Domain.Model;
using Voguedi.Cqrs.Samples.RabbitMQ.MemroyCache.SqlServer.Stores;

namespace Voguedi.Cqrs.Samples.RabbitMQ.MemroyCache.SqlServer.Commands.Handlers
{
    class NoteCommandHandler
        : ICommandHandler<CreateNoteCommand>
        , ICommandAsyncHandler<TryModifyNoteCommand>
        , ICommandHandler<ModifyNoteCommand>
    {
        #region Private Fields

        readonly INoteStore store;

        #endregion

        #region Ctors
        
        public NoteCommandHandler(INoteStore store) => this.store = store;

        #endregion

        #region ICommandHandler<CreateNoteCommand>

        public Task HandleAsync(ICommandHandlerContext context, CreateNoteCommand command)
            => context.CreateAggregateRootAsync<Note, string>(new Note(command.AggregateRootId, command.Title, command.Content));

        #endregion

        #region ICommandAsyncHandler<TryModifyNoteCommand>

        public async Task<AsyncExecutedResult<IApplicationMessage>> HandleAsync(TryModifyNoteCommand command)
        {
            var result = await store.GetAsync(command.AggregateRootId);

            if (result.Succeeded)
            {
                if (result.Data != null)
                    return AsyncExecutedResult<IApplicationMessage>.Success(new ModifyNoteApplicationMessage(command.AggregateRootId, command.Title, command.Content));

                return AsyncExecutedResult<IApplicationMessage>.Success(null);
            }

            return AsyncExecutedResult<IApplicationMessage>.Failed(result.Exception);
        }

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
