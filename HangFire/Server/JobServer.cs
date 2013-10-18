using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using ServiceStack.Logging;
using ServiceStack.Redis;

namespace HangFire.Server
{
    internal class JobServer : IDisposable
    {
        private readonly ServerContext _context;
        private readonly int _workerCount;
        private readonly IEnumerable<string> _queues;
        private readonly TimeSpan _pollInterval;

        private readonly Thread _serverThread;

        private JobManager _manager;
        private ThreadWrapper _schedulePoller;
        private ThreadWrapper _fetchedJobsWatcher;

        private readonly IRedisClient _redis = RedisFactory.CreateClient();
        private readonly ManualResetEvent _stopped = new ManualResetEvent(false);

        private readonly ILog _logger = LogManager.GetLogger("HangFire.Manager");

        public JobServer(
            string machineName,
            int workerCount,
            IEnumerable<string> queues, 
            TimeSpan pollInterval,
            JobActivator jobActivator)
        {
            _workerCount = workerCount;
            _queues = queues;
            _pollInterval = pollInterval;

            if (queues == null) throw new ArgumentNullException("queues");

            if (pollInterval != pollInterval.Duration())
            {
                throw new ArgumentOutOfRangeException("pollInterval", "Poll interval value must be positive.");
            }

            var serverName = String.Format("{0}:{1}", machineName, Process.GetCurrentProcess().Id);

            _context = new ServerContext(
                serverName,
                jobActivator ?? new JobActivator(),
                JobPerformer.Current);

            _serverThread = new Thread(RunServer)
                {
                    Name = typeof(JobServer).Name,
                    IsBackground = true
                };
            _serverThread.Start();
        }

        public void Dispose()
        {
            _stopped.Set();
            _serverThread.Join();
        }

        private void StartServer()
        {
            _manager = new JobManager(_context, _workerCount, _queues);
            _schedulePoller = new ThreadWrapper(new SchedulePoller(_pollInterval));
            _fetchedJobsWatcher = new ThreadWrapper(new DequeuedJobsWatcher());
        }

        private void StopServer()
        {
            _fetchedJobsWatcher.Dispose();
            _schedulePoller.Dispose();
            _manager.Dispose();
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Unexpected exception should not fail the whole application.")]
        private void RunServer()
        {
            try
            {
                AnnounceServer();
                StartServer();

                _stopped.WaitOne();

                StopServer();
                RemoveServer();
            }
            catch (Exception ex)
            {
                _logger.Fatal("Unexpected exception caught.", ex);
            }
        }

        private void AnnounceServer()
        {
            using (var transaction = _redis.CreateTransaction())
            {
                transaction.QueueCommand(x => x.AddItemToSet(
                    "hangfire:servers", _context.ServerName));

                transaction.QueueCommand(x => x.SetRangeInHash(
                    String.Format("hangfire:server:{0}", _context.ServerName),
                    new Dictionary<string, string>
                        {
                            { "WorkerCount", _workerCount.ToString() },
                            { "StartedAt", JobHelper.ToStringTimestamp(DateTime.UtcNow) }
                        }));

                foreach (var queue in _queues)
                {
                    var queue1 = queue;
                    transaction.QueueCommand(x => x.AddItemToList(
                        String.Format("hangfire:server:{0}:queues", _context.ServerName),
                        queue1));
                }

                transaction.Commit();
            }
        }

        private void RemoveServer()
        {
            using (var transaction = _redis.CreateTransaction())
            {
                transaction.QueueCommand(x => x.RemoveItemFromSet(
                    "hangfire:servers",
                    _context.ServerName));

                transaction.QueueCommand(x => x.RemoveEntry(
                    String.Format("hangfire:server:{0}", _context.ServerName),
                    String.Format("hangfire:server:{0}:queues", _context.ServerName)));

                transaction.Commit();
            }
        }
    }
}