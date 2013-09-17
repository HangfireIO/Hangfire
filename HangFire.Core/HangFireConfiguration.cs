namespace HangFire
{
    /// <summary>
    /// Represents the common configuration for HangFire clients and servers.
    /// </summary>
    public class JobStorageConfiguration
    {
        internal JobStorageConfiguration()
        {
            RedisHost = "localhost";
            RedisPort = 6379;
            RedisPassword = null;
            RedisDb = 0;
        }

        /// <summary>
        /// Gets or sets Redis hostname. Default: "localhost"
        /// </summary>
        public string RedisHost { get; set; }

        /// <summary>
        /// Gets or sets Redis port. Default: 6379
        /// </summary>
        public int RedisPort { get; set; }

        /// <summary>
        /// Gets or sets Redis password. Default: null
        /// </summary>
        public string RedisPassword { get; set; }

        /// <summary>
        /// Gets or sets Redis database number. Default: 0
        /// </summary>
        public long RedisDb { get; set; }
    }
}
