namespace Voguedi.ApplicationMessages
{
    public interface IProcessingApplicationMessageQueueFactory
    {
        #region Methods

        IProcessingApplicationMessageQueue Create(string routingKey);

        #endregion
    }
}
