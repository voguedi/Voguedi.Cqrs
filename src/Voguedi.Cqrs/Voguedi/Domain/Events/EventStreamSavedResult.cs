namespace Voguedi.Domain.Events
{
    public enum EventStreamSavedResult
    {
        Success,
        Failed,
        DuplicatedEvent,
        DuplicatedCommand
    }
}
