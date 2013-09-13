using System;
using System.Collections.Generic;
using System.ComponentModel;
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
            where TJob : HangFireJob
        {
            return PerformAsync<TJob>(null);
        }

        public static string PerformAsync<TJob>(object args)
            where TJob : HangFireJob
        {
            return PerformAsync(typeof(TJob), args);
        }

        public static string PerformAsync(Type jobType, object args = null)
        {
            return Instance.Async(jobType, args);
        }

        public static string PerformIn<TJob>(TimeSpan interval)
            where TJob : HangFireJob
        {
            return PerformIn<TJob>(interval, null);
        }

        public static string PerformIn<TJob>(TimeSpan interval, object args)
            where TJob : HangFireJob
        {
            return PerformIn(interval, typeof(TJob), args);
        }

        public static string PerformIn(TimeSpan interval, Type jobType, object args = null)
        {
            return Instance.In(interval, jobType, args);
        }

        private readonly RedisStorage _redis = new RedisStorage();
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
            if (!typeof (HangFireJob).IsAssignableFrom(jobType))
            {
                throw new ArgumentException(
                    String.Format("The type '{0}' must inherit '{1}'.", jobType, typeof(HangFireJob)), 
                    "jobType");
            }

            var jobId = GenerateId();
            var job = InitializeJob(jobType, args);

            Action enqueueAction = () =>
            {
                var queueName = JobHelper.GetQueueName(jobType);

                lock (_redis)
                {
                    _redis.EnqueueJob(queueName, jobId, job);
                }
            };

            InvokeFilters(jobId, job, enqueueAction);

            return jobId;
        }

        public string In(TimeSpan interval, Type jobType, object args = null)
        {
            if (jobType == null)
            {
                throw new ArgumentNullException("jobType");
            }
            if (!typeof(HangFireJob).IsAssignableFrom(jobType))
            {
                throw new ArgumentException(
                    String.Format("The type '{0}' must inherit '{1}'.", jobType, typeof(HangFireJob)),
                    "jobType");
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

            var jobId = GenerateId();
            var job = InitializeJob(jobType, args);

            Action enqueueAction = () =>
            {
                lock (_redis)
                {
                    _redis.ScheduleJob(jobId, job, at);
                }
            };

            InvokeFilters(jobId, job, enqueueAction);

            return jobId;
        }

        public void Dispose()
        {
            _redis.Dispose();
        }

        private Dictionary<string, string> InitializeJob(Type jobType, object args)
        {
            var job = new Dictionary<string, string>();
            job["Type"] = jobType.AssemblyQualifiedName;
            job["Args"] = SerializeArgs(args);

            return job;
        }

        private string GenerateId()
        {
            return Guid.NewGuid().ToString();
        }

        private string SerializeArgs(object args)
        {
            var dictionary = new Dictionary<string, string>();

            if (args != null)
            {
                foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(args))
                {
                    var propertyValue = descriptor.GetValue(args);
                    string value = null;

                    if (propertyValue != null)
                    {
                        // TODO: handle conversion exception and display it in a friendly way.
                        var converter = TypeDescriptor.GetConverter(propertyValue.GetType());
                        value = converter.ConvertToInvariantString(propertyValue);
                    }

                    dictionary.Add(descriptor.Name, value);
                }
            }

            return JsonHelper.Serialize(dictionary);
        }

        private void InvokeFilters(
            string jobId,
            Dictionary<string, string> job,
            Action enqueueAction)
        {
            var commandAction = enqueueAction;

            var entries = _filters.ToList();
            entries.Reverse();

            foreach (var entry in entries)
            {
                var currentEntry = entry;

                var filterContext = new ClientFilterContext(jobId, job, commandAction);
                commandAction = () => currentEntry.ClientFilter(filterContext);
            }

            commandAction();
        }
    }
}
