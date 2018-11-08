using System;

namespace Voguedi.Messaging
{
    public interface IMessageService : IDisposable
    {
        #region Methods

        void Start();

        #endregion
    }
}
