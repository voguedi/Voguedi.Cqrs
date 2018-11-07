using System;

namespace Voguedi.Events
{
    public sealed class EventStreamDescriptor
    {
        #region Public Properties

        public string Id { get; set; }

        public DateTime Timestamp { get; set; }

        public string CommandId { get; set; }

        public string AggregateRootTypeName { get; set; }

        public string AggregateRootId { get; set; }

        public long Version { get; set; }

        public string Events { get; set; }

        #endregion
    }
}
