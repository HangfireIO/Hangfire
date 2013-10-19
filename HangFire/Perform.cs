using System;
using System.Diagnostics.CodeAnalysis;
using HangFire.Client;

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
            using (var client = new JobClient(RedisFactory.PooledManager))
            {
                return client.Async(jobType, args);
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
                return client.In(interval, jobType, args);
            }
        }
    }
}
