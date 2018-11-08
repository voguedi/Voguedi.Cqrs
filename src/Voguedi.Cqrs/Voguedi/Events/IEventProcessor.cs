using System;

namespace Voguedi.Events
{
    public interface IEventProcessor : IDisposable
    {
        #region Methods

        void Process(ProcessingEvent processingEvent);

        void Start();

        #endregion
    }
}
