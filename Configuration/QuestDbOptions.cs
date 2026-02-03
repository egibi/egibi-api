namespace egibi_api.Configuration
{
    /// <summary>
    /// QuestDB connection and endpoint configuration.
    /// Bound from appsettings.json "QuestDb" section.
    /// </summary>
    public class QuestDbOptions
    {
        public const string SectionName = "QuestDb";

        /// <summary>
        /// HTTP API + Web Console URL (e.g., "http://localhost:9000").
        /// Used for schema init and REST queries.
        /// </summary>
        public string HttpUrl { get; set; } = "http://localhost:9000";

        /// <summary>
        /// ILP (InfluxDB Line Protocol) host for data ingestion.
        /// </summary>
        public string IlpHost { get; set; } = "localhost";

        /// <summary>
        /// ILP port (default: 9009).
        /// </summary>
        public int IlpPort { get; set; } = 9009;
    }
}
