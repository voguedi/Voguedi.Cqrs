using System.Threading.Tasks;

namespace Voguedi.ApplicationMessages
{
    public interface IProcessingApplicationMessageHandler
    {
        #region Methods

        Task HandleAsync(ProcessingApplicationMessage processingApplicationMessage);

        #endregion
    }
}
