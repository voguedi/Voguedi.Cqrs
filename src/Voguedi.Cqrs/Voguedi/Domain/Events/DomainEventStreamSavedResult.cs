namespace Voguedi.Domain.Events
{
    public enum DomainEventStreamSavedResult
    {
        Success,
        Failed,
        DuplicatedDomainEvent,
        DuplicatedCommand
    }
}
