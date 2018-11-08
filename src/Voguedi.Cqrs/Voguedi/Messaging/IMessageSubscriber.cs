using System;

namespace Voguedi.Messaging
{
    public interface IMessageSubscriber : IDisposable
    {
        #region Methods

        void Start();

        #endregion
    }
}
