using System;
using System.ComponentModel;
using HangFire.Client;
using HangFire.States;
using ServiceStack.Redis;

namespace HangFire
{
    /// <summary>
    /// <p>The top-level class of the HangFire Client part. Provides several
    /// static methods to create jobs using guids as a unique identifier.</p>
    /// <p>All methods are thread-safe and use the <see cref="PooledRedisClientManager"/> 
    /// to take pooled Redis connections when creating a job.</p>
    /// </summary>
    public static class Perform
    {
        /// <summary>
        /// Enqueues a new argumentless job of the <typeparamref name="TJob"/> 
        /// type to its queue.
        /// </summary>
        /// 
        /// <typeparam name="TJob">Type of the job.</typeparam>
        /// <returns>The unique identifier of the job.</returns>
        /// 
        /// <exception cref="ArgumentException">The <see cref="BackgroundJob"/> type is not assignable from the given <typeparamref name="TJob"/>.</exception>
        /// <exception cref="InvalidOperationException">The <typeparamref name="TJob"/> has invalid queue name.</exception>
        /// <exception cref="CreateJobFailedException">Creation of the job was failed.</exception>
        public static string Async<TJob>()
            where TJob : BackgroundJob
        {
            return Async<TJob>(null);
        }

        /// <summary>
        /// Enqueues a new job of the <typeparamref name="TJob"/> type to its
        /// queue with the specified arguments in the <paramref name="args"/> parameter.
        /// </summary>
        /// 
        /// <typeparam name="TJob">Type of the job</typeparam>
        /// <param name="args">Job arguments.</param>
        /// <returns>The unique identifier of the job.</returns>
        /// 
        /// <exception cref="ArgumentException">The <see cref="BackgroundJob"/> type is not assignable from the given <typeparamref name="TJob"/>.</exception>
        /// <exception cref="InvalidOperationException">The <typeparamref name="TJob"/> has invalid queue name.</exception>
        /// <exception cref="InvalidOperationException">Could not serialize one or more properties of the <paramref name="args"/> object using its <see cref="TypeConverter"/>.</exception>
        /// <exception cref="CreateJobFailedException">Creation of the job was failed.</exception>
        public static string Async<TJob>(object args)
            where TJob : BackgroundJob
        {
            return Async(typeof(TJob), args);
        }

        /// <summary>
        /// Enqueues a new argumentless job of the specified type to its queue.
        /// </summary>
        /// 
        /// <param name="type">Type of the job.</param>
        /// <returns>The unique identifier of the job.</returns>
        /// 
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is null.</exception>
        /// <exception cref="ArgumentException">The <see cref="BackgroundJob"/> type is not assignable from the given <paramref name="type"/>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the <paramref name="type"/> has invalid queue name.</exception>
        /// <exception cref="CreateJobFailedException">Thrown when job creation was failed.</exception>
        public static string Async(Type type)
        {
            return Async(type, null);
        }

        /// <summary>
        /// Enqueues a new job of the specified type to its queue with the 
        /// given arguments in the <paramref name="args"/> parameter.
        /// </summary>
        /// 
        /// <param name="type">Type of the job.</param>
        /// <param name="args">Job arguments.</param>
        /// <returns>The unique identifier of the job.</returns>
        /// 
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is null.</exception>
        /// <exception cref="ArgumentException">The <see cref="BackgroundJob"/> type is not assignable from the given <paramref name="type"/>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the <paramref name="type"/> has invalid queue name.</exception>
        /// <exception cref="InvalidOperationException">Could not serialize one or more properties of the <paramref name="args"/> object using its <see cref="TypeConverter"/>.</exception>
        /// <exception cref="CreateJobFailedException">Thrown when job creation was failed.</exception>
        public static string Async(Type type, object args)
        {
            using (var client = new JobClient(RedisFactory.PooledManager))
            {
                var enqueuedState = new EnqueuedState("Enqueued by the Сlient");
                var uniqueId = GenerateId();
                
                client.CreateJob(uniqueId, type, enqueuedState, args);
                return uniqueId;
            }
        }

        /// <summary>
        /// Schedules a new argumentless job of the specified type to perform 
        /// after the given <paramref name="delay"/>.
        /// </summary>
        /// 
        /// <typeparam name="TJob">The type of the job.</typeparam>
        /// <param name="delay">Delay, after which the job should be performed.</param>
        /// <returns>The unique identifier of the job.</returns>
        /// 
        /// <exception cref="ArgumentException">The <see cref="BackgroundJob"/> type is not assignable from the given <typeparamref name="TJob"/>.</exception>
        /// <exception cref="CreateJobFailedException">Thrown when job creation was failed.</exception>
        public static string In<TJob>(TimeSpan delay)
            where TJob : BackgroundJob
        {
            return In<TJob>(delay, null);
        }

        /// <summary>
        /// Schedules a new job of the specified type to perform after the 
        /// given <paramref name="delay"/> with the arguments defined in 
        /// the <paramref name="args"/> parameter.
        /// </summary>
        /// 
        /// <typeparam name="TJob">The type of the job.</typeparam>
        /// <param name="delay">Delay, after which the job should be performed.</param>
        /// <param name="args">Job arguments.</param>
        /// <returns>The unique identifier of the job.</returns>
        /// 
        /// <exception cref="ArgumentException">The <see cref="BackgroundJob"/> type is not assignable from the given <typeparamref name="TJob"/>.</exception>
        /// <exception cref="InvalidOperationException">Could not serialize one or more properties of the <paramref name="args"/> object using its <see cref="TypeConverter"/>.</exception>
        /// <exception cref="CreateJobFailedException">Thrown when job creation was failed.</exception>
        public static string In<TJob>(TimeSpan delay, object args)
            where TJob : BackgroundJob
        {
            return In(delay, typeof(TJob), args);
        }

        /// <summary>
        /// Schedules a new argumentless job of the specified type to perform 
        /// after the given <paramref name="delay"/>.
        /// </summary>
        /// 
        /// <param name="delay">Delay, after which the job should be performed.</param>
        /// <param name="type">The type of the job.</param>
        /// <returns>The unique identifier of the job.</returns>
        /// 
        /// <exception cref="ArgumentException">The <see cref="BackgroundJob"/> type is not assignable from the given <paramref name="type"/>.</exception>
        /// <exception cref="CreateJobFailedException">Thrown when job creation was failed.</exception>
        public static string In(TimeSpan delay, Type type)
        {
            return In(delay, type, null);
        }

        /// <summary>
        /// Schedules a new job of the specified type to perform after the given
        /// <paramref name="delay"/> with the arguments defined in the
        /// <paramref name="args"/> parameter.
        /// </summary>
        /// 
        /// <param name="delay">Delay, after which the job should be performed.</param>
        /// <param name="type">The type of the job.</param>
        /// <param name="args">Job arguments.</param>
        /// <returns>The unique identifier of the job.</returns>
        /// 
        /// <exception cref="ArgumentException">The <see cref="BackgroundJob"/> type is not assignable from the given <paramref name="type"/>.</exception>
        /// <exception cref="InvalidOperationException">Could not serialize one or more properties of the <paramref name="args"/> object using the <see cref="TypeConverter"/>.</exception>
        /// <exception cref="CreateJobFailedException">Thrown when job creation was failed.</exception>
        public static string In(TimeSpan delay, Type type, object args)
        {
            using (var client = new JobClient(RedisFactory.BasicManager))
            {
                var scheduledState = new ScheduledState("Scheduled by the Client", DateTime.UtcNow.Add(delay));
                var uniqueId = GenerateId();

                client.CreateJob(uniqueId, type, scheduledState, args);
                return uniqueId;
            }
        }

        /// <summary>
        /// Generates a unique identifier for the job.
        /// </summary>
        /// <returns>Unique identifier for the job.</returns>
        private static string GenerateId()
        {
            return Guid.NewGuid().ToString();
        }
    }
}
