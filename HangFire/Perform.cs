using System;
using System.Diagnostics.CodeAnalysis;
using HangFire.Client;
using HangFire.States;

namespace HangFire
{
    public static class Perform
    {
        [SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
        public static string Async<TJob>()
            where TJob : BackgroundJob
        {
            return Async<TJob>(null);
        }

        [SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
        public static string Async<TJob>(object args)
            where TJob : BackgroundJob
        {
            return Async(typeof(TJob), args);
        }

        public static string Async(Type jobType)
        {
            return Async(jobType, null);
        }

        public static string Async(Type jobType, object args)
        {
            if (jobType == null)
            {
                throw new ArgumentNullException("jobType");
            }

            using (var client = new JobClient(RedisFactory.PooledManager))
            {
                var queue = JobHelper.GetQueue(jobType);
                var enqueuedState = new EnqueuedState("Enqueued by the Сlient", queue);

                return client.CreateJob(jobType, enqueuedState, args);
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
        public static string In<TJob>(TimeSpan interval)
            where TJob : BackgroundJob
        {
            return In<TJob>(interval, null);
        }

        [SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
        public static string In<TJob>(TimeSpan interval, object args)
            where TJob : BackgroundJob
        {
            return In(interval, typeof(TJob), args);
        }

        public static string In(TimeSpan interval, Type jobType)
        {
            return In(interval, jobType, null);
        }

        public static string In(TimeSpan interval, Type jobType, object args)
        {
            using (var client = new JobClient(RedisFactory.PooledManager))
            {
                var scheduledState = new ScheduledState("Scheduled by the Client", DateTime.UtcNow.Add(interval));
                return client.CreateJob(jobType, scheduledState, args);
            }
        }
    }
}
