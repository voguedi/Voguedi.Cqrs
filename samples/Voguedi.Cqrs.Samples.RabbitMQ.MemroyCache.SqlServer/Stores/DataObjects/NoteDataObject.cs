using System;

namespace Voguedi.Cqrs.Samples.RabbitMQ.MemroyCache.SqlServer.Stores.DataObjects
{
    public class NoteDataObject
    {
        #region Public Properties

        public string Id { get; set; }

        public long Version { get; set; }

        public string Title { get; set; }

        public string Content { get; set; }

        public DateTime CreatedOn { get; set; }

        public DateTime? ModifiedOn { get; set; }

        #endregion
    }
}
