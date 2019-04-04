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

        Task ProcessAsync(ProcessingCommand processingCommand, CommandExecutedResult executedResult);

        bool IsInactive(int expiration);

        #endregion
    }
}
