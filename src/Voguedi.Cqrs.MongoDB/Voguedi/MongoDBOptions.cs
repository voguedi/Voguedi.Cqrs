namespace Voguedi
{
    public class MongoDBOptions
    {
        #region Public Properties

        public string ConnectionString { get; set; } = "mongodb://localhost:27017";

        public string DatabaseName { get; set; }

        public string EventCollectionName { get; set; } = "Events";

        public string EventVersionCollectionName { get; set; } = "EventVersions";

        #endregion
    }
}
