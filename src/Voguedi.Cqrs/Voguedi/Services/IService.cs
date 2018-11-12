using System;

namespace Voguedi.Services
{
    public interface IService : IDisposable
    {
        #region Methods

        void Start();

        #endregion
    }
}
