using System.Threading.Tasks;

namespace Voguedi.Domain.Events
{
    public class ProcessingEvent
    {
        #region Ctors

        public ProcessingEvent(EventStream stream) => Stream = stream;

        #endregion

        #region Public Properties

        public EventStream Stream { get; }

        public IProcessingEventQueue Queue { get; set; }

        #endregion

        #region Public Methods

        public void EnqueueToWaitingQueue() => Queue.EnqueueToWaiting(this);

        public Task OnQueueProcessedAsync() => Queue.ProcessAsync(this);

        #endregion
    }
}
