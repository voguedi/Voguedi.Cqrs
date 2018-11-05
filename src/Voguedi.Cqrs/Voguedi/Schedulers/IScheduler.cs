using System;

namespace Voguedi.Schedulers
{
    public interface IScheduler
    {
        #region Methods

        void Start(string key, Action action, int dueTime, int period);

        void Stop(string key);

        #endregion
    }
}
