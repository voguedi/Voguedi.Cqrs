namespace Voguedi.ActiveCheckers
{
    public interface IMemoryQueueActiveContext
    {
        #region Methods

        bool IsInactive(int expiration);

        #endregion
    }
}
