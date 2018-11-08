using System;
using System.Threading.Tasks;

namespace Voguedi.Events
{
    public interface IEventCommitter : IDisposable
    {
        #region Methods

        Task CommitAsync(CommittingEvent committingEvent);

        void Start();

        #endregion
    }
}
