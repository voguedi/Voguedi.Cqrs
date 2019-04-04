using System;
using System.Collections.Generic;
using System.Linq;
using Voguedi.Infrastructure;

namespace Voguedi.Domain.Events
{
    public class EventStream
    {
        #region Ctors

        public EventStream(long id, DateTime timestamp, long commandId, string aggregateRootTypeName, string aggregateRootId, long version, IReadOnlyList<IEvent> events)
        {
            foreach (var e in events)
            {
                if (e.Version != version)
                    throw new ArgumentException($"聚合根版本与事件版本不同。 [AggregateRootType = {aggregateRootTypeName}, AggregateRootId = {aggregateRootId}, AggregateRootVersion = {version}, EventType = {e.GetType()}, EventId = {e.Id}, EventVersion = {e.Version}]", nameof(events));
            }

            Id = id;
            Timestamp = timestamp;
            Version = version;
            CommandId = commandId;
            AggregateRootTypeName = aggregateRootTypeName;
            AggregateRootId = aggregateRootId;
            Events = events;
        }

        public EventStream(long commandId, string aggregateRootTypeName, string aggregateRootId, long version, IReadOnlyList<IEvent> events)
            : this(SnowflakeId.Default().NewId(), DateTime.UtcNow, commandId, aggregateRootTypeName, aggregateRootId, version, events) { }

        #endregion

        #region Public Properties

        public long Id { get; }

        public DateTime Timestamp { get; }

        public long CommandId { get; }

        public string AggregateRootTypeName { get; }

        public string AggregateRootId { get; }

        public long Version { get; }

        public IReadOnlyList<IEvent> Events { get; }

        #endregion

        #region Public Methods

        public override string ToString() => $"[Id = {Id}, Timestamp = {Timestamp}, CommandId = {CommandId}, AggregateRootTypeName = {AggregateRootTypeName}, AggregateRootId = {AggregateRootId}, Version = {Version}, Events = [{string.Join(" | ", Events.Select(e => $"Type = {e.GetType()}, Id = {e.Id}"))}]]";

        #endregion
    }
}
