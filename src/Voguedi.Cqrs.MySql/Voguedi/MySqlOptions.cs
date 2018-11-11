namespace Voguedi
{
    public sealed class MySqlOptions
    {
        #region Public Properties

        public string ConnectionString { get; set; }

        public string Schema { get; set; } = "dbo";

        public int TableCount { get; set; } = 1;

        #endregion
    }
}
