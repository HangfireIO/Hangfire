// This file is part of Hangfire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// Hangfire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// Hangfire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with Hangfire. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Concurrent;
using System.Threading;
using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.Logging;
using Hangfire.Profiling;
using Hangfire.States;
using Hangfire.Storage;

namespace Hangfire.Server
{
    /// <summary>
    /// Represents a background process responsible for <i>enqueueing delayed
    /// jobs</i>.
    /// </summary>
    /// 
    /// <remarks>
    /// <para>This background process polls the <i>delayed job schedule</i> for 
    /// delayed jobs that are ready to be enqueued. To prevent a stress load
    /// on a job storage, the configurable delay is used between scheduler 
    /// runs. Delay is used only when there are no more background jobs to be
    /// enqueued.</para>
    /// 
    /// <para>When a background job is ready to be enqueued, it is simply
    /// moved from <see cref="ScheduledState"/> to the <see cref="EnqueuedState"/>
    /// by using <see cref="IBackgroundJobStateChanger"/>.</para>
    /// 
    /// <para>Delayed job schedule is based on a Set data structure of a job
    /// storage, so you can use this background process as an example of a
    /// custom extension.</para>
    ///  
    /// <para>Multiple instances of this background process can be used in
    /// separate threads/processes without additional configuration (distributed
    /// locks are used). However, this only adds support for fail-over, and does 
    /// not increase the performance.</para>
    /// 
    /// <note type="important">
    /// If you are using <b>custom filter providers</b>, you need to pass a custom
    /// <see cref="IBackgroundJobStateChanger"/> instance to make this process
    /// respect your filters when enqueueing background jobs.
    /// </note>
    /// </remarks>
    /// 
    /// <threadsafety static="true" instance="true"/>
    /// 
    /// <seealso cref="ScheduledState"/>
    public class DelayedJobScheduler : IBackgroundProcess
    {
        /// <summary>
        /// Represents a default polling interval for delayed job scheduler. 
        /// This field is read-only.
        /// </summary>
        /// <remarks>
        /// The value of this field is <c>TimeSpan.FromSeconds(15)</c>.
        /// </remarks>
        public static readonly TimeSpan DefaultPollingDelay = TimeSpan.FromSeconds(15);
        private static readonly TimeSpan DefaultLockTimeout = TimeSpan.FromMinutes(1);
        private static readonly int BatchSize = 1000;

        private readonly ILog _logger = LogProvider.For<DelayedJobScheduler>();
        private readonly ConcurrentDictionary<Type, bool> _isBatchingAvailableCache = new ConcurrentDictionary<Type, bool>();

        private readonly IBackgroundJobStateChanger _stateChanger;
        private readonly IProfiler _profiler;
        private readonly TimeSpan _pollingDelay;

        /// <summary>
        /// Initializes a new instance of the <see cref="DelayedJobScheduler"/>
        /// class with the <see cref="DefaultPollingDelay"/> value as a
        /// delay between runs.
        /// </summary>
        public DelayedJobScheduler() 
            : this(DefaultPollingDelay)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DelayedJobScheduler"/>
        /// class with a specified polling interval.
        /// </summary>
        /// <param name="pollingDelay">Delay between scheduler runs.</param>
        public DelayedJobScheduler(TimeSpan pollingDelay)
            : this(pollingDelay, new BackgroundJobStateChanger())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DelayedJobScheduler"/>
        /// class with a specified polling interval and given state changer.
        /// </summary>
        /// <param name="pollingDelay">Delay between scheduler runs.</param>
        /// <param name="stateChanger">State changer to use for background jobs.</param>
        /// 
        /// <exception cref="ArgumentNullException"><paramref name="stateChanger"/> is null.</exception>
        public DelayedJobScheduler(TimeSpan pollingDelay, [NotNull] IBackgroundJobStateChanger stateChanger)
        {
            if (stateChanger == null) throw new ArgumentNullException(nameof(stateChanger));

            _stateChanger = stateChanger;
            _pollingDelay = pollingDelay;
            _profiler = new SlowLogProfiler(_logger);
        }

        /// <inheritdoc />
        public void Execute(BackgroundProcessContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var jobsEnqueued = 0;

            while (EnqueueNextScheduledJobs(context))
            {
                jobsEnqueued++;

                if (context.IsStopping)
                {
                    break;
                }
            }

            if (jobsEnqueued != 0)
            {
                _logger.Debug($"{jobsEnqueued} scheduled job(s) enqueued.");
            }

            context.Wait(_pollingDelay);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return GetType().Name;
        }

        private bool EnqueueNextScheduledJobs(BackgroundProcessContext context)
        {
            return UseConnectionDistributedLock(context.Storage, connection =>
            {
                if (IsBatchingAvailable(connection))
                {
                    var timestamp = JobHelper.ToTimestamp(DateTime.UtcNow);
                    var jobIds = ((JobStorageConnection)connection).GetFirstByLowestScoreFromSet("schedule", 0, timestamp, BatchSize);

                    if (jobIds == null || jobIds.Count == 0) return false;

                    foreach (var jobId in jobIds)
                    {
                        if (context.IsStopping) return false;
                        EnqueueBackgroundJob(context, connection, jobId);
                    }
                }
                else
                {
                    for (var i = 0; i < BatchSize; i++)
                    {
                        if (context.IsStopping) return false;

                        var timestamp = JobHelper.ToTimestamp(DateTime.UtcNow);

                        var jobId = connection.GetFirstByLowestScoreFromSet("schedule", 0, timestamp);
                        if (jobId == null) return false;

                        EnqueueBackgroundJob(context, connection, jobId);
                    }
                }

                return true;
            });
        }

        private void EnqueueBackgroundJob(BackgroundProcessContext context, IStorageConnection connection, string jobId)
        {
            var appliedState = _stateChanger.ChangeState(new StateChangeContext(
                context.Storage,
                connection,
                jobId,
                new EnqueuedState { Reason = $"Triggered by {ToString()}" },
                new [] { ScheduledState.StateName },
                CancellationToken.None,
                _profiler));

            if (appliedState == null)
            {
                // When a background job with the given id does not exist, we should
                // remove its id from a schedule manually. This may happen when someone
                // modifies a storage bypassing Hangfire API.
                using (var transaction = connection.CreateWriteTransaction())
                {
                    transaction.RemoveFromSet("schedule", jobId);
                    transaction.Commit();
                }
            }
        }

        private bool IsBatchingAvailable(IStorageConnection connection)
        {
            return _isBatchingAvailableCache.GetOrAdd(
                connection.GetType(),
                type =>
                {
                    if (connection is JobStorageConnection storageConnection)
                    {
                        try
                        {
                            storageConnection.GetFirstByLowestScoreFromSet(null, 0, 0, 1);
                        }
                        catch (ArgumentNullException ex) when (ex.ParamName == "key")
                        {
                            return true;
                        }
                        catch (Exception)
                        {
                            //
                        }
                    }

                    return false;
                });
        }

        private T UseConnectionDistributedLock<T>(JobStorage storage, Func<IStorageConnection, T> action)
        {
            var resource = "locks:schedulepoller";
            try
            {
                using (var connection = storage.GetConnection())
                using (connection.AcquireDistributedLock(resource, DefaultLockTimeout))
                {
                    return action(connection);
                }
            }
            catch (DistributedLockTimeoutException e) when (e.Resource.EndsWith(resource))
            {
                // DistributedLockTimeoutException here doesn't mean that delayed jobs weren't enqueued.
                // It just means another Hangfire server did this work.
                _logger.DebugException(
                    $@"An exception was thrown during acquiring distributed lock on the {resource} resource within {DefaultLockTimeout.TotalSeconds} seconds. The scheduled jobs have not been handled this time.
It will be retried in {_pollingDelay.TotalSeconds} seconds", 
                    e);
                return default(T);
            }
        }
    }
}