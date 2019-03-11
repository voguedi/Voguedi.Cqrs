using System;

namespace Voguedi.Services
{
    public interface ISubscriberService : IDisposable
    {
        #region Methods

        void Start();

        #endregion
    }
}
