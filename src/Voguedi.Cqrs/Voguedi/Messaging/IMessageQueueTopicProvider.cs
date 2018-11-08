namespace Voguedi.Messaging
{
    public interface IMessageQueueTopicProvider
    {
        #region Methods

        string Get(IMessage message, string defaultGroupName, int defaultTopicQueueCount);

        #endregion
    }
}
