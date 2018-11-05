namespace Voguedi.Events
{
    public interface IEventProcessor
    {
        #region Methods

        void Process(ProcessingEvent processingEvent);

        #endregion
    }
}
