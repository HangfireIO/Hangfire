using System;
using System.Threading;

namespace HangFire.Server
{
    public class JobServer : IServerComponent
    {
        private readonly JobStorage _storage;
        private readonly string _serverId;
        private readonly ServerContext _context;
        private readonly Lazy<IServerComponentRunner> _runner;

        public JobServer(
            string serverId,
            ServerContext context,
            JobStorage storage,
            Lazy<IServerComponentRunner> runner)
        {
            if (storage == null) throw new ArgumentNullException("storage");
            if (serverId == null) throw new ArgumentNullException("serverId");
            if (context == null) throw new ArgumentNullException("context");
            if (runner == null) throw new ArgumentNullException("runner");

            _storage = storage;
            _serverId = serverId;
            _context = context;
            _runner = runner;
        }

        public void Execute(CancellationToken cancellationToken)
        {
            using (var connection = _storage.GetConnection())
            {
                connection.AnnounceServer(_serverId, _context);
            }

            try
            {
                using (_runner.Value)
                {
                    _runner.Value.Start();

                    cancellationToken.WaitHandle.WaitOne();
                }
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
