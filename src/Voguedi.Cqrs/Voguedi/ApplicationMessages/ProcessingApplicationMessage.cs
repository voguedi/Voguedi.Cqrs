using System.Threading.Tasks;

namespace Voguedi.ApplicationMessages
{
    public class ProcessingApplicationMessage
    {
        #region Ctors

        public ProcessingApplicationMessage(IApplicationMessage applicationMessage) => ApplicationMessage = applicationMessage;

        #endregion

        #region Public Properties

        public IApplicationMessage ApplicationMessage { get; }

        public IProcessingApplicationMessageQueue Queue { get; set; }

        #endregion

        #region Public Methods

        public Task OnQueueProcessedAsync() => Queue.ProcessAsync();

        #endregion
    }
}
