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

namespace Hangfire.Server
{
    internal static class BackgroundProcessExtensions
    {
        public static void Execute(this ILongRunningProcess process, BackgroundProcessContext context)
        {
            if (!(process is IServerComponent || process is IBackgroundProcess))
            {
                throw new ArgumentOutOfRangeException("process", "Long-running process must be of type IServerComponent or IBackgroundProcess.");
            }

            var backgroundProcess = process as IBackgroundProcess;
            if (backgroundProcess != null)
            {
                backgroundProcess.Execute(context);
            }
            else
            {
                var component = process as IServerComponent;
                if (component != null)
                {
                    component.Execute(context.CancellationToken);
                }
            }
        }

        public static Task CreateTask([NotNull] this ILongRunningProcess process, BackgroundProcessContext context)
        {
            if (process == null) throw new ArgumentNullException("process");

            if (!(process is IServerComponent || process is IBackgroundProcess))
            {
                throw new ArgumentOutOfRangeException("process", "Long-running process must be of type IServerComponent or IBackgroundProcess.");
            }

            return Task.Factory.StartNew(
                () => RunProcess(process, context),
                TaskCreationOptions.LongRunning);
        }

        private static void RunProcess(ILongRunningProcess process, BackgroundProcessContext context)
        {
            // Long-running tasks are based on custom threads (not threadpool ones) as in 
            // .NET Framework 4.5, so we can try to set custom thread name to simplify the
            // debugging experience.
            TrySetThreadName(process.ToString());

            // LogProvider.GetLogger does not throw any exception, that is why we are not
            // using the `try` statement here. It does not return `null` value as well.
            var logger = LogProvider.GetLogger(process.ToString());
            logger.DebugFormat("Server component '{0}' started.", process);

            try
            {
                process.Execute(context);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                logger.FatalException(
                    String.Format(
                        "Fatal error occurred during execution of '{0}' component. It will be stopped. See the exception for details.",
                        process),
                    ex);
            }

            logger.DebugFormat("Server component '{0}' stopped.", process);
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