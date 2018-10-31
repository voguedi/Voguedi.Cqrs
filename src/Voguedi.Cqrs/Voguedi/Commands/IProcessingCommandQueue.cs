using System.Threading.Tasks;

namespace Voguedi.Commands
{
    public interface IProcessingCommandQueue
    {
        #region Methods

        void Enqueue(ProcessingCommand processingCommand);

        void Pause();

        void ResetSequence(long sequence);

        void Restart();

        Task CommitAsync(ProcessingCommand processingCommand);

        Task RejectAsync(ProcessingCommand processingCommand);

        #endregion
    }
}
