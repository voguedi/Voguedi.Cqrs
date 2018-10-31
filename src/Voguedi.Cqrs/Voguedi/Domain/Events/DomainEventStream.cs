using System;
using System.Collections.Generic;
using System.Linq;
using Voguedi.IdentityGeneration;

namespace Voguedi.Domain.Events
{
    public sealed class DomainEventStream
    {
        #region Public Properties

        public string Id { get; }

        public DateTime Timestamp { get; }

        public string CommandId { get; }

        public string AggregateRootTypeName { get; }

        public string AggregateRootId { get; }

        public long Version { get; }

        public IReadOnlyList<IDomainEvent> Events { get; }

        #endregion

        #region Ctors
        
        public DomainEventStream(string id, DateTime timestamp, string commandId, string aggregateRootTypeName, string aggregateRootId, long version, IReadOnlyList<IDomainEvent> events)
        {
            foreach (var e in events)
            {
                if (e.Version != version)
                    throw new ArgumentNullException(nameof(events), $"领域事件流版本与领域事件版本不同！ [AggregateRootTypeName = {aggregateRootTypeName}, AggregateRootId = {aggregateRootId}, Version = {version}, EventVersion = {e.Version}]");
            }

            Id = id;
            Timestamp = timestamp;
            Version = version;
            CommandId = commandId;
            AggregateRootTypeName = aggregateRootTypeName;
            AggregateRootId = aggregateRootId;
            Events = events;
        }

        public DomainEventStream(string commandId, string aggregateRootTypeName, string aggregateRootId, long version, IReadOnlyList<IDomainEvent> events)
            : this(StringIdentityGenerator.Instance.Generate(), DateTime.UtcNow, commandId, aggregateRootTypeName, aggregateRootId, version, events)
        { }

        #endregion

        #region Public Methods

        public override string ToString() => $"[Id = {Id}, Timestamp = {Timestamp}, CommandId = {CommandId}, AggregateRootTypeName = {AggregateRootTypeName}, AggregateRootId = {AggregateRootId}, Version = {Version}, Events = [{string.Join(" | ", Events.Select(e => $"Type = {e.GetType()}, Id = {e.Id}"))}]]";

        #endregion
    }
}
