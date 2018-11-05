using System;

namespace Voguedi.Processors
{
    public interface IProcessor : IDisposable
    {
        #region Methods

        void Start();

        #endregion
    }
}
