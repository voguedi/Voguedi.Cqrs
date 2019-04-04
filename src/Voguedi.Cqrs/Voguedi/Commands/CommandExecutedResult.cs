namespace Voguedi.Commands
{
    public class CommandExecutedResult
    {
        #region Ctors

        public CommandExecutedResult(CommandExecutedStatus status, long commandId, string aggregateRootId, string message = null, string messageType = null)
        {
            Status = status;
            CommandId = commandId;
            AggregateRootId = aggregateRootId;
            Message = message;

            if (!string.IsNullOrWhiteSpace(messageType))
                MessageType = messageType;
            else
                MessageType = typeof(string).FullName;
        }

        #endregion

        #region Public Properties

        public CommandExecutedStatus Status { get; }

        public long CommandId { get; }

        public string AggregateRootId { get; }

        public string Message { get; }

        public string MessageType { get; }

        #endregion
    }
}
