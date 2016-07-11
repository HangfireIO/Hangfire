// This file is part of Hangfire.
// Copyright © 2015 Sergey Odinokov.
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
using System.Threading;
using System.Threading.Tasks;
using Hangfire.Annotations;
using Hangfire.Logging;

#pragma warning disable 618

namespace Hangfire.Server
{
    internal static class ServerProcessExtensions
    {
        public static void Execute(this IServerProcess process, BackgroundProcessContext context)
        {
            if (!(process is IServerComponent || process is IBackgroundProcess))
            {
                throw new ArgumentOutOfRangeException(nameof(process), "Long-running process must be of type IServerComponent or IBackgroundProcess.");
            }

            var backgroundProcess = process as IBackgroundProcess;
            if (backgroundProcess != null)
            {
                backgroundProcess.Execute(context);
            }
            else
            {
                var component = (IServerComponent) process;
                component.Execute(context.CancellationToken);
            }
        }

        public static Task CreateTask([NotNull] this IServerProcess process, BackgroundProcessContext context)
        {
            if (process == null) throw new ArgumentNullException(nameof(process));

            if (!(process is IServerComponent || process is IBackgroundProcess))
            {
                throw new ArgumentOutOfRangeException(nameof(process), "Long-running process must be of type IServerComponent or IBackgroundProcess.");
            }

            return Task.Factory.StartNew(
                () => RunProcess(process, context),
                TaskCreationOptions.LongRunning);
        }

        public static Type GetProcessType([NotNull] this IServerProcess process)
        {
            if (process == null) throw new ArgumentNullException(nameof(process));

            var nextProcess = process;

            while (nextProcess is IBackgroundProcessWrapper)
            {
                nextProcess = ((IBackgroundProcessWrapper) nextProcess).InnerProcess;
            }

            return nextProcess.GetType();
        }

        private static void RunProcess(IServerProcess process, BackgroundProcessContext context)
        {
            // Long-running tasks are based on custom threads (not threadpool ones) as in 
            // .NET Framework 4.5, so we can try to set custom thread name to simplify the
            // debugging experience.
            TrySetThreadName(process.ToString());

            // LogProvider.GetLogger does not throw any exception, that is why we are not
            // using the `try` statement here. It does not return `null` value as well.
            var logger = LogProvider.GetLogger(process.GetProcessType());
            logger.Debug($"Background process '{process}' started.");

            try
            {
                process.Execute(context);
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException && context.IsShutdownRequested)
                {
                    // Graceful shutdown
                    logger.Trace($"Background process '{process}' was stopped due to a shutdown request.");
                }
                else
                {
                    logger.FatalException(
                        $"Fatal error occurred during execution of '{process}' process. It will be stopped. See the exception for details.",
                        ex);
                }
            }

            logger.Debug($"Background process '{process}' stopped.");
        }

        private static void TrySetThreadName(string name)
        {
            try
            {
                Thread.CurrentThread.Name = name;
            }
            catch (InvalidOperationException)
            {
            }
        }
    }
}