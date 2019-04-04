namespace Voguedi
{
    public class SqlServerOptions
    {
        #region Public Properties

        public string ConnectionString { get; set; }

        public string Schema { get; set; } = "dbo";

        public string EventTableName { get; set; } = "Events";

        public int EventTableCount { get; set; } = 1;

        public string EventVersionTableName { get; set; } = "EventVersions";

        public int EventVersionTableCount { get; set; } = 1;

        #endregion
    }
}
