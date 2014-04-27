using System;
using System.Threading;

namespace HangFire.Server
{
    public class JobServer2 : IServerComponent, IServerComponentRunner
    {
        private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(5);

        private readonly JobStorage _storage;
        private readonly string _serverId;
        private readonly IServerComponentRunner _runner;

        public JobServer2(
            string serverId,
            ServerContext context,
            JobStorage storage, 
            IServerComponentRunner runner)
        {
            if (storage == null) throw new ArgumentNullException("storage");
            if (serverId == null) throw new ArgumentNullException("serverId");
            if (context == null) throw new ArgumentNullException("context");
            if (runner == null) throw new ArgumentNullException("runner");

            _storage = storage;
            _serverId = serverId;
            _runner = runner;

            using (var connection = _storage.GetConnection())
            {
                connection.AnnounceServer(serverId, context);
            }
        }

        public void Execute(CancellationToken cancellationToken)
        {
            using (var connection = _storage.GetConnection())
            {
                connection.Heartbeat(_serverId);
            }

            cancellationToken.WaitHandle.WaitOne(HeartbeatInterval);
        }

        public void Start()
        {
            _runner.Start();
        }

        public void Stop()
        {
            _runner.Stop();
        }

        public void Dispose()
        {
            try
            {
                _runner.Dispose();
            }
            finally 
            {
                using (var connection = _storage.GetConnection())
                {
                    connection.RemoveServer(_serverId);
                }
            }
        }
    }
}
