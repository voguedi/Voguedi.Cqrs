using System.Threading.Tasks;
using Voguedi.AsyncExecution;
using Voguedi.Cqrs.Samples.RabbitMQ.MemroyCache.SqlServer.Stores;
using Voguedi.Domain.Events;

namespace Voguedi.Cqrs.Samples.RabbitMQ.MemroyCache.SqlServer.Domain.Events.Handlers
{
    class NoteEventHandler
        : IEventHandler<NoteCreatedEvent>
        , IEventHandler<NoteModifiedEvent>
    {
        #region Private Fields

        readonly INoteStore store;

        #endregion

        #region Ctors

        public NoteEventHandler(INoteStore store) => this.store = store;

        #endregion

        #region IEventHandler<NoteCreatedEvent>

        public Task<AsyncExecutedResult> HandleAsync(NoteCreatedEvent e) => store.CreateAsync(e.AggregateRootId, e.Version, e.Title, e.Content, e.Timestamp);

        #endregion

        #region IEventHandler<NoteModifiedEvent>

        public Task<AsyncExecutedResult> HandleAsync(NoteModifiedEvent e) => store.ModifyAsync(e.AggregateRootId, e.Version, e.Title, e.Content, e.Timestamp);

        #endregion
    }
}
