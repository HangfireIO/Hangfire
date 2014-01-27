// This file is part of HangFire.
// Copyright © 2013 Sergey Odinokov.
// 
// HangFire is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// HangFire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with HangFire.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using HangFire.Common;
using HangFire.Server.Components;
using HangFire.Server.Fetching;
using HangFire.Server.Performing;
using ServiceStack.Logging;
using ServiceStack.Redis;

namespace HangFire.Server
{
    internal class JobServer : IDisposable
    {
        private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(5);
        private const int RetryAttempts = 10;

        private static readonly ILog Logger = LogManager.GetLogger(typeof(JobServer));

        private readonly ServerContext _context;
        private readonly int _workerCount;
        private readonly IEnumerable<string> _queues;
        private readonly TimeSpan _pollInterval;
        private readonly TimeSpan _fetchTimeout;

        private readonly Thread _serverThread;

        private ThreadWrapper _manager;
        private ThreadWrapper _schedulePoller;
        private ThreadWrapper _dequeuedJobsWatcher;
        private ThreadWrapper _serverWatchdog;

        private readonly IRedisClientsManager _redisManager;
        private readonly IRedisClient _redis;

        private readonly ManualResetEvent _stopped = new ManualResetEvent(false);

        public JobServer(
            IRedisClientsManager redisManager,
            string serverName,
            int workerCount,
            IEnumerable<string> queues,
            TimeSpan pollInterval,
            TimeSpan fetchTimeout)
        {
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

            _redis = redisManager.GetClient();

            _context = new ServerContext(
                serverName,
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
            _dequeuedJobsWatcher = new ThreadWrapper(new DequeuedJobsWatcher(_redisManager));
            _serverWatchdog = new ThreadWrapper(new ServerWatchdog(_redisManager));
        }

        private void StopServer()
        {
            _serverWatchdog.Dispose();
            _dequeuedJobsWatcher.Dispose();
            _schedulePoller.Dispose();
            _manager.Dispose();
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Unexpected exception should not fail the whole application.")]
        private void RunServer()
        {
            try
            {
                Logger.Info("Starting HangFire Server...");

                AnnounceServer();
                StartServer();

                Logger.Info("HangFire Server has been started.");

                while (true)
                {
                    RetryOnException(Heartbeat, _stopped);

                    if (_stopped.WaitOne(HeartbeatInterval))
                    {
                        break;
                    }
                }

                Logger.Info("Stopping HangFire Server...");

                StopServer();
                RemoveServer(_redis, _context.ServerName);

                Logger.Info("HangFire Server has been stopped.");
            }
            catch (Exception ex)
            {
                Logger.Fatal("Unexpected exception caught.", ex);
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