using System.Threading.Tasks;
using Voguedi.AsyncExecution;

namespace Voguedi.Events
{
    public interface IEventHandler { }

    public interface IEventHandler<in TEvent> : IEventHandler
        where TEvent : class, IEvent
    {
        #region Methods

        Task<AsyncExecutedResult> HandleAsync(TEvent e);

        #endregion
    }
}
