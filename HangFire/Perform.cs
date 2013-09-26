using System;
using HangFire.Client;

namespace HangFire
{
    public class Perform
    {
        private static readonly JobClient Client = new JobClient();

        static Perform()
        {
        }

        public static string Async<TJob>()
            where TJob : BackgroundJob
        {
            return Async<TJob>(null);
        }

        public static string Async<TJob>(object args)
            where TJob : BackgroundJob
        {
            return Async(typeof(TJob), args);
        }

        public static string Async(Type jobType, object args = null)
        {
            return Client.Async(jobType, args);
        }

        public static string In<TJob>(TimeSpan interval)
            where TJob : BackgroundJob
        {
            return In<TJob>(interval, null);
        }

        public static string In<TJob>(TimeSpan interval, object args)
            where TJob : BackgroundJob
        {
            return In(interval, typeof(TJob), args);
        }

        public static string In(TimeSpan interval, Type jobType, object args = null)
        {
            return Client.In(interval, jobType, args);
        }
    }
}
