using System;

namespace Voguedi.Commands
{
    public interface ICommandProcessor : IDisposable
    {
        #region Methods

        void Process(ProcessingCommand processingCommand);

        void Start();

        #endregion
    }
}
