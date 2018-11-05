namespace Voguedi.Events
{
    public interface IEventCommitter
    {
        #region Methods

        void Commit(CommittingEvent committingEvent);

        #endregion
    }
}
