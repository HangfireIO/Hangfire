using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using ServiceStack.Logging;
using ServiceStack.Redis;

namespace HangFire.Server
{
    internal class JobServer : IDisposable
    {
        private const int RetryAttempts = 10;

        private readonly ServerContext _context;
        private readonly int _workerCount;
        private readonly IEnumerable<string> _queues;
        private readonly TimeSpan _pollInterval;
        private readonly TimeSpan _fetchTimeout;

        private readonly Thread _serverThread;

        private ThreadWrapper _manager;
        private ThreadWrapper _schedulePoller;
        private ThreadWrapper _fetchedJobsWatcher;
        private ThreadWrapper _serverWatchdog;

        private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(5);

        private readonly IRedisClientsManager _redisManager;
        private readonly IRedisClient _redis;

        private readonly ManualResetEvent _stopped = new ManualResetEvent(false);

        private readonly ILog _logger = LogManager.GetLogger("HangFire.Manager");

        public JobServer(
            IRedisClientsManager redisManager,
            string serverName,
            int workerCount,
            IEnumerable<string> queues,
            JobActivator jobActivator,
            TimeSpan pollInterval,
            TimeSpan fetchTimeout)
        {
            _redis = redisManager.GetClient();

            _redisManager = redisManager;
            _workerCount = workerCount;
            _queues = queues;
            _pollInterval = pollInterval;
            _fetchTimeout = fetchTimeout;

            if (queues == null) throw new ArgumentNullException("queues");

            if (pollInterval != pollInterval.Duration())
            {
                throw new ArgumentOutOfRangeException("pollInterval", "Poll interval value must be positive.");
            }

            _context = new ServerContext(
                serverName,
                jobActivator ?? new JobActivator(),
                new JobPerformer());

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
            _manager = new ThreadWrapper(new WorkerManager(
                new PrioritizedJobFetcher(_redisManager, _queues, _workerCount, _fetchTimeout),
                _redisManager,
                _context,
                _workerCount));

            _schedulePoller = new ThreadWrapper(new SchedulePoller(_redisManager, _pollInterval));
            _fetchedJobsWatcher = new ThreadWrapper(new DequeuedJobsWatcher(_redisManager));
            _serverWatchdog = new ThreadWrapper(new ServerWatchdog(_redisManager));
        }

        private void StopServer()
        {
            _serverWatchdog.Dispose();
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

                while (true)
                {
                    RetryOnException(Heartbeat, _stopped);

                    if (_stopped.WaitOne(HeartbeatInterval))
                    {
                        break;
                    }
                }

                StopServer();
                RemoveServer(_redis, _context.ServerName);
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
                            { "StartedAt", JobHelper.ToStringTimestamp(DateTime.UtcNow) },
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

        private void Heartbeat()
        {
            _redis.SetEntryInHash(
                String.Format("hangfire:server:{0}", _context.ServerName),
                "Heartbeat",
                JobHelper.ToStringTimestamp(DateTime.UtcNow));
        }

        public static void RemoveServer(IRedisClient redis, string serverName)
        {
            using (var transaction = redis.CreateTransaction())
            {
                transaction.QueueCommand(x => x.RemoveItemFromSet(
                    "hangfire:servers",
                    serverName));

                transaction.QueueCommand(x => x.RemoveEntry(
                    String.Format("hangfire:server:{0}", serverName),
                    String.Format("hangfire:server:{0}:queues", serverName)));

                transaction.Commit();
            }
        }

        public static void RetryOnException(Action action, WaitHandle waitHandle)
        {
            for (var i = 0; i < RetryAttempts; i++)
            {
                try
                {
                    action();

                    // Break the loop after successful invocation.
                    break;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch
                {
                    // Break the loop after the retry attempts number exceeded.
                    if (i == RetryAttempts - 1) throw;

                    // Break the loop when the wait handle was signaled.
                    if (SleepBackOffMultiplier(i, waitHandle))
                    {
                        break;
                    }
                }
            }
        }

        private static bool SleepBackOffMultiplier(int i, WaitHandle waitHandle)
        {
            //exponential/random retry back-off.
            var rand = new Random(Guid.NewGuid().GetHashCode());
            var nextTry = rand.Next(
                (int)Math.Pow(i, 2), (int)Math.Pow(i + 1, 2) + 1);

            return waitHandle.WaitOne(TimeSpan.FromSeconds(nextTry));
        }
    }
}