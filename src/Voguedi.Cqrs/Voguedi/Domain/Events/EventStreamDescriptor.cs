using System;

namespace Voguedi.Domain.Events
{
    public sealed class EventStreamDescriptor
    {
        #region Public Properties

        public long Id { get; set; }

        public DateTime Timestamp { get; set; }

        public long CommandId { get; set; }

        public string AggregateRootTypeName { get; set; }

        public string AggregateRootId { get; set; }

        public long Version { get; set; }

        public string Events { get; set; }

        #endregion
    }
}
