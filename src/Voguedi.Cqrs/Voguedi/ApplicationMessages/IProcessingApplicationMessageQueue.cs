using System.Threading.Tasks;

namespace Voguedi.ApplicationMessages
{
    public interface IProcessingApplicationMessageQueue
    {
        #region Methods

        void Enqueue(ProcessingApplicationMessage processingApplicationMessage);

        Task ProcessAsync();

        bool IsInactive(int expiration);

        #endregion
    }
}
