using System;
using System.Collections.Generic;

namespace HangFire
{
    /// <summary>
    /// Represents the common configuration for HangFire clients and servers.
    /// </summary>
    public class HangFireConfiguration
    {
        /// <summary>
        /// Gets the current HangFire configuration.
        /// </summary>
        public static HangFireConfiguration Current { get; private set; }

        static HangFireConfiguration()
        {
            Current = new HangFireConfiguration();
        }

        /// <summary>
        /// Runs specified configuration action to configure HangFire.
        /// </summary>
        /// <param name="action">Configuration action.</param>
        public static void Configure(Action<HangFireConfiguration> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException("action");
            }

            action(Current);
        }

        internal HangFireConfiguration()
        {
            RedisHost = "localhost";
            RedisPort = 6379;
            RedisPassword = null;
            RedisDb = 0;

            ServerFilters = new List<IServerJobFilter>();
            ClientFilters = new List<IClientJobFilter>();

            AddFilter(new CurrentCultureFilter());
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

        internal IList<IServerJobFilter> ServerFilters { get; private set; }
        internal IList<IClientJobFilter> ClientFilters { get; private set; }

        public void AddFilter(IJobFilter jobFilter)
        {
            if (jobFilter == null)
            {
                throw new ArgumentNullException("jobFilter");
            }

            var serverFilter = jobFilter as IServerJobFilter;
            if (serverFilter != null)
            {
                ServerFilters.Add(serverFilter);
            }

            var clientFilter = jobFilter as IClientJobFilter;
            if (clientFilter != null)
            {
                ClientFilters.Add(clientFilter);
            }
        }

        public void ClearFilters()
        {
            ClientFilters.Clear();
            ServerFilters.Clear();
        }
    }
}
