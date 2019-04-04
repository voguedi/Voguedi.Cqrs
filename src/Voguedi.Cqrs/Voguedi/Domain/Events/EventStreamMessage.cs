using System.Collections.Generic;
using System.Linq;
using Voguedi.Messaging;

namespace Voguedi.Domain.Events
{
    public class EventStreamMessage : Message
    {
        #region Public Properties

        public long CommandId { get; set; }

        public string AggregateRootTypeName { get; set; }

        public string AggregateRootId { get; set; }

        public long Version { get; set; }

        public IReadOnlyDictionary<string, string> Events { get; set; }

        #endregion

        #region Message

        public override string GetRoutingKey() => AggregateRootId;

        #endregion

        #region Public Methods

        public override string ToString() => $"[Id = {Id}, Timestamp = {Timestamp}, CommandId = {CommandId}, AggregateRootTypeName = {AggregateRootTypeName}, AggregateRootId = {AggregateRootId}, Version = {Version}, Events = [{string.Join(" | ", Events.Select(e => $"Type = {e.Key}, Content = {e.Value}"))}]]";

        #endregion
    }
}
