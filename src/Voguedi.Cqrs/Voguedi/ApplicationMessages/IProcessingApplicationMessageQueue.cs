using System.Threading.Tasks;

namespace Voguedi.ApplicationMessages
{
    public interface IProcessingApplicationMessageQueue
    {
        #region Methods

        void Enqueue(ProcessingApplicationMessage processingApplicationMessage);

        Task CommitAsync(ProcessingApplicationMessage processingApplicationMessage);

        Task RejectAsync(ProcessingApplicationMessage processingApplicationMessage);

        bool IsInactive(int expiration);

        #endregion
    }
}
