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
            WorkerActivator = new WorkerActivator();
            
            RedisHost = "localhost";
            RedisPort = 6379;
            RedisPassword = null;
            RedisDb = 0;

            ServerFilters = new List<IServerFilter>();
            ClientFilters = new List<IClientFilter>();

            AddFilter(new I18NFilter());
        }

        /// <summary>
        /// Gets or sets the instance of <see cref="WorkerActivator"/> that
        /// will be used to as <see cref="Worker"/> instances resolver.
        /// </summary>
        public WorkerActivator WorkerActivator { get; set; }

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

        public IList<IServerFilter> ServerFilters { get; private set; }
        public IList<IClientFilter> ClientFilters { get; private set; }

        public void AddFilter(IFilter filter)
        {
            var serverFilter = filter as IServerFilter;
            if (serverFilter != null)
            {
                ServerFilters.Add(serverFilter);
            }

            var clientFilter = filter as IClientFilter;
            if (clientFilter != null)
            {
                ClientFilters.Add(clientFilter);
            }
        }
    }
}
