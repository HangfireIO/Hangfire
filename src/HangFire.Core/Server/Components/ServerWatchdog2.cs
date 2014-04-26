using System;
using System.Threading;
using Common.Logging;

namespace HangFire.Server.Components
{
    public class ServerWatchdog2 : IServerComponent
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ServerWatchdog2));

        private readonly JobStorage _storage;
        private readonly ServerWatchdogOptions _options;

        public ServerWatchdog2(JobStorage storage)
            : this(storage, new ServerWatchdogOptions())
        {
        }

        public ServerWatchdog2(JobStorage storage, ServerWatchdogOptions options)
        {
            if (storage == null) throw new ArgumentNullException("storage");
            if (options == null) throw new ArgumentNullException("options");

            _storage = storage;
            _options = options;
        }

        public void Execute(CancellationToken cancellationToken)
        {
            using (var connection = _storage.GetConnection())
            {
                var serversRemoved = connection.RemoveTimedOutServers(_options.ServerTimeout);
                if (serversRemoved != 0)
                {
                    Logger.Info(String.Format(
                        "{0} servers were removed due to timeout", 
                        serversRemoved));
                }
            }

            cancellationToken.WaitHandle.WaitOne(_options.CheckInterval);
        }
    }
}