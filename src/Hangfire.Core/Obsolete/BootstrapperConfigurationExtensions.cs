// This file is part of Hangfire. Copyright © 2013-2014 Sergey Odinokov.
// 
// Permission to use, copy, modify, and/or distribute this software for any
// purpose with or without fee is hereby granted.
// 
// THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
// REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY
// AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
// INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM
// LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR
// OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR
// PERFORMANCE OF THIS SOFTWARE.

using System;

namespace Hangfire
{
    /// <exclude />
    [Obsolete("Please use `AppBuilderExtensions` class instead. Will be removed in version 2.0.0.")]
    public static class BootstrapperConfigurationExtensions
    {
        /// <summary>
        /// Tells bootstrapper to start a job server with default options
        /// on application start and stop it automatically on application
        /// shutdown request. Global job storage is used.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        [Obsolete("Please use `IAppBuilder.UseHangfireServer` OWIN extension method instead. Will be removed in version 2.0.0.")]
        public static void UseServer(this IBootstrapperConfiguration configuration)
        {
            configuration.UseServer(() => new BackgroundJobServer());
        }

        /// <summary>
        /// Tells bootstrapper to start a job server with the given
        /// amount of workers on application start and stop it automatically
        /// on application shutdown request. Global job storage is used.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="workerCount">Worker count</param>
        [Obsolete("Please use `IAppBuilder.UseHangfireServer` OWIN extension method instead. Will be removed in version 2.0.0.")]
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
        /// on application shutdown request. Global job storage is used.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="queues">Queues to listen</param>
        [Obsolete("Please use `IAppBuilder.UseHangfireServer` OWIN extension method instead. Will be removed in version 2.0.0.")]
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
        /// Global job storage is used.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="workerCount">Worker count</param>
        /// <param name="queues">Queues to listen</param>
        [Obsolete("Please use `IAppBuilder.UseHangfireServer` OWIN extension method instead. Will be removed in version 2.0.0.")]
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
        /// on application shutdown request. Global job storage is used.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="options">Job server options</param>
        [Obsolete("Please use `IAppBuilder.UseHangfireServer` OWIN extension method instead. Will be removed in version 2.0.0.")]
        public static void UseServer(
            this IBootstrapperConfiguration configuration,
            BackgroundJobServerOptions options)
        {
            configuration.UseServer(() => new BackgroundJobServer(options));
        }

        /// <summary>
        /// Tells bootstrapper to start a job server, that uses
        /// the given job storage, on application start and stop
        /// it automatically on application shutdown request.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="storage">Job storage to use</param>
        [Obsolete("Please use `IAppBuilder.UseHangfireServer` OWIN extension method instead. Will be removed in version 2.0.0.")]
        public static void UseServer(
            this IBootstrapperConfiguration configuration,
            JobStorage storage)
        {
            configuration.UseServer(() => new BackgroundJobServer(
                new BackgroundJobServerOptions(),
                storage));
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
        [Obsolete("Please use `IAppBuilder.UseHangfireServer` OWIN extension method instead. Will be removed in version 2.0.0.")]
        public static void UseServer(
            this IBootstrapperConfiguration configuration,
            JobStorage storage,
            BackgroundJobServerOptions options)
        {
            configuration.UseServer(() => new BackgroundJobServer(options, storage));
        }
    }
}
