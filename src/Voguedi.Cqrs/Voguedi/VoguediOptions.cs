namespace Voguedi
{
    public sealed class VoguediOptions
    {
        #region Public Properties

        public string DefaultCommandGroupName { get; set; } = "CommandGroup";

        public string DefaultEventGroupName { get; set; } = "EventGroup";

        public int DefaultTopicQueueCount { get; set; } = 1;

        public int MemoryQueueActiveExpiration { get; set; } = 5000;

        #endregion
    }
}
