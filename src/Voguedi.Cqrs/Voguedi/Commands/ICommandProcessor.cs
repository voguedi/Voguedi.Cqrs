using System;
using System.Threading.Tasks;

namespace Voguedi.Commands
{
    public interface ICommandProcessor : IDisposable
    {
        #region Methods

        Task ProcessAsync(ProcessingCommand processingCommand);

        void Start();

        #endregion
    }
}
