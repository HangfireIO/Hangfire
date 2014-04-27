using System;
using System.Threading;

namespace HangFire.Server
{
    public class ServerHeartbeat : IServerComponent
    {
        private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(5);

        private readonly JobStorage _storage;
        private readonly string _serverId;

        public ServerHeartbeat(JobStorage storage, string serverId)
        {
            if (storage == null) throw new ArgumentNullException("storage");
            if (serverId == null) throw new ArgumentNullException("serverId");

            _storage = storage;
            _serverId = serverId;
        }

        public void Execute(CancellationToken cancellationToken)
        {
            using (var connection = _storage.GetConnection())
            {
                connection.Heartbeat(_serverId);
            }

            cancellationToken.WaitHandle.WaitOne(HeartbeatInterval);
        }
    }
}