namespace Voguedi.Events
{
    public enum EventStreamSavedResult
    {
        Success,
        Failed,
        DuplicatedEvent,
        DuplicatedCommand
    }
}
