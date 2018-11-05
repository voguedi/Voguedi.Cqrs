using System;
using System.Threading.Tasks;

namespace Voguedi.Events
{
    public interface IEventProcessor : IDisposable
    {
        #region Methods

        Task ProcessAsync(ProcessingEvent processingEvent);

        void Start();

        #endregion
    }
}
