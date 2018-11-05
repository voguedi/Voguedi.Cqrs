using System;

namespace Voguedi.ActiveCheckers
{
    public interface IMemoryQueueActiveChecker
    {
        #region Methods

        void Start(string key, Action action, int expiration);

        void Stop(string key);

        #endregion
    }
}
