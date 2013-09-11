using System;
using System.Collections.Generic;
using System.Linq;

namespace HangFire
{
    /// <summary>
    /// Represents a top-level class for enqueuing jobs.
    /// </summary>
    public class HangFireClient : IDisposable
    {
        private static readonly HangFireClient Instance = new HangFireClient(
            HangFireConfiguration.Current.ClientFilters);

        static HangFireClient()
        {
        }

        public static string PerformAsync<TJob>()
        {
            return PerformAsync<TJob>(null);
        }

        public static string PerformAsync<TJob>(object args)
        {
            return PerformAsync(typeof(TJob), args);
        }

        public static string PerformAsync(Type jobType, object args = null)
        {
            return Instance.Async(jobType, args);
        }

        public static string PerformIn<TJob>(TimeSpan interval)
        {
            return PerformIn<TJob>(interval, null);
        }

        public static string PerformIn<TJob>(TimeSpan interval, object args)
        {
            return PerformIn(interval, typeof(TJob), args);
        }

        public static string PerformIn(TimeSpan interval, Type jobType, object args = null)
        {
            return Instance.In(interval, jobType, args);
        }

        private readonly RedisClient _client = new RedisClient();
        private readonly IEnumerable<IClientFilter> _filters;

        internal HangFireClient(IEnumerable<IClientFilter> filters)
        {
            _filters = filters;
        }

        public string Async(Type jobType, object args = null)
        {
            if (jobType == null)
            {
                throw new ArgumentNullException("jobType");
            }

            var jobDescription = new JobDescription(jobType, args);

            Action enqueueAction = () =>
            {
                var serializedDescription = jobDescription.Serialize();
                var queue = JobHelper.GetQueueName(jobType);

                lock (_client)
                {
                    _client.TryToDo(storage => storage.EnqueueJob(queue, serializedDescription), throwOnError: true);
                }
            };

            InvokeFilters(jobDescription, enqueueAction);

            return jobDescription.Jid;
        }

        public string In(TimeSpan interval, Type jobType, object args = null)
        {
            if (jobType == null)
            {
                throw new ArgumentNullException("jobType");
            }

            if (interval != interval.Duration())
            {
                throw new ArgumentOutOfRangeException("interval", "Interval value can not be negative.");
            }

            if (interval.Equals(TimeSpan.Zero))
            {
                return Async(jobType, args);
            }

            var at = DateTime.UtcNow.Add(interval).ToTimestamp();

            var jobDescription = new JobDescription(jobType, args);

            Action enqueueAction = () =>
            {
                var serializedDescription = jobDescription.Serialize();

                lock (_client)
                {
                    _client.TryToDo(
                        storage => storage.ScheduleJob(serializedDescription, at),
                        throwOnError: true);
                }
            };

            InvokeFilters(jobDescription, enqueueAction);

            return jobDescription.Jid;
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        private void InvokeFilters(
            JobDescription jobDescription,
            Action enqueueAction)
        {
            var commandAction = enqueueAction;

            var entries = _filters.ToList();
            entries.Reverse();

            foreach (var entry in entries)
            {
                var currentEntry = entry;

                var filterContext = new ClientFilterContext(jobDescription, commandAction);
                commandAction = () => currentEntry.ClientFilter(filterContext);
            }

            commandAction();
        }
    }
}
