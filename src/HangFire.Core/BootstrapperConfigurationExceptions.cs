// This file is part of HangFire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// HangFire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// HangFire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with HangFire. If not, see <http://www.gnu.org/licenses/>.

namespace HangFire
{
    public static class BootstrapperConfigurationExceptions
    {
        /// <summary>
        /// Tells bootstrapper to start a job server with default options
        /// on application start and stop it automatically on application
        /// shutdown request. Global job storage is being used.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        public static void UseServer(this IBootstrapperConfiguration configuration)
        {
            configuration.UseServer(() => new BackgroundJobServer());
        }

        /// <summary>
        /// Tells bootstrapper to start a job server with the given
        /// amount of workers on application start and stop it automatically
        /// on application shutdown request. Global job storage is being used.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="workerCount">Worker count</param>
        public static void UseServer(
            this IBootstrapperConfiguration configuration,
            int workerCount)
        {
            var options = new BackgroundJobServerOptions
            {
                WorkerCount = workerCount
            };

            configuration.UseServer(() => new BackgroundJobServer(options));
        }

        /// <summary>
        /// Tells bootstrapper to start a job server with the given
        /// queues array on application start and stop it automatically
        /// on application shutdown request. Global job storage is being used.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="queues">Queues to listen</param>
        public static void UseServer(
            this IBootstrapperConfiguration configuration,
            params string[] queues)
        {
            var options = new BackgroundJobServerOptions
            {
                Queues = queues
            };

            configuration.UseServer(() => new BackgroundJobServer(options));
        }

        /// <summary>
        /// Tells bootstrapper to start a job server with the given
        /// queues array and specified amount of workers on application
        /// start and stop it automatically on application shutdown request.
        /// Global job storage is being used.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="workerCount">Worker count</param>
        /// <param name="queues">Queues to listen</param>
        public static void UseServer(
            this IBootstrapperConfiguration configuration,
            int workerCount,
            params string[] queues)
        {
            var options = new BackgroundJobServerOptions
            {
                WorkerCount = workerCount,
                Queues = queues
            };

            configuration.UseServer(() => new BackgroundJobServer(options));
        }

        /// <summary>
        /// Tells bootstrapper to start a job server with the given
        /// options on application start and stop it automatically
        /// on application shutdown request. Global job storage is being used.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="options">Job server options</param>
        public static void UseServer(
            this IBootstrapperConfiguration configuration,
            BackgroundJobServerOptions options)
        {
            configuration.UseServer(() => new BackgroundJobServer(options));
        }

        /// <summary>
        /// Tells bootstrapper to start a job server with the given
        /// options that use the specified storage (not the global one) on
        /// application start and stop it automatically on application
        /// shutdown request.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="storage">Job storage to use</param>
        /// <param name="options">Job server options</param>
        public static void UseServer(
            this IBootstrapperConfiguration configuration,
            JobStorage storage,
            BackgroundJobServerOptions options)
        {
            configuration.UseServer(() => new BackgroundJobServer(options, storage));
        }
    }
}
