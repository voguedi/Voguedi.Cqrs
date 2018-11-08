using System.Collections.Generic;
using Voguedi.Messaging;

namespace Voguedi.Events
{
    public sealed class EventStreamMessage : Message
    {
        #region Public Properties

        public string CommandId { get; set; }

        public string AggregateRootTypeName { get; set; }

        public string AggregateRootId { get; set; }

        public long Version { get; set; }

        public IReadOnlyDictionary<string, string> Events { get; set; }

        #endregion

        #region Message

        public override string GetRoutingKey() => AggregateRootId;

        #endregion
    }
}
