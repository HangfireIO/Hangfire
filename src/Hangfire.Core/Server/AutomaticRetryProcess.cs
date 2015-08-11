// This file is part of Hangfire.
// Copyright © 2013-2014 Sergey Odinokov.
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
using Hangfire.Annotations;
using Hangfire.Logging;

namespace Hangfire.Server
{
    internal class AutomaticRetryProcess : IBackgroundProcess
    {
        private static readonly TimeSpan DefaultMaxAttemptDelay = TimeSpan.FromMinutes(5);
        private const int DefaultMaxRetryAttempts = int.MaxValue;

        private readonly IServerProcess _innerProcess;
        private readonly ILog _logger;

        public AutomaticRetryProcess([NotNull] IServerProcess innerProcess)
        {
            if (innerProcess == null) throw new ArgumentNullException("innerProcess");

            _innerProcess = innerProcess;
            _logger = LogProvider.GetLogger(_innerProcess.GetType());
            
            MaxRetryAttempts = DefaultMaxRetryAttempts;
            MaxAttemptDelay = DefaultMaxAttemptDelay;
            DelayCallback = GetBackOffMultiplier;
        }

        public int MaxRetryAttempts { get; set; }
        public TimeSpan MaxAttemptDelay { get; set; }
        public Func<int, TimeSpan> DelayCallback { get; set; }

        public IServerProcess InnerProcess
        {
            get { return _innerProcess; }
        }

        public void Execute(BackgroundProcessContext context)
        {
            for (var i = 0; i <= MaxRetryAttempts; i++)
            {
                try
                {
                    _innerProcess.Execute(context);
                    return;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    // Break the loop after the retry attempts number exceeded.
                    if (i >= MaxRetryAttempts - 1) throw;

                    var nextTry = DelayCallback(i);
                    var logLevel = GetLogLevel(i);

                    _logger.Log(
                        logLevel,
                        () => String.Format(
                            "Error occurred during execution of '{0}' component. Execution will be retried (attempt {1} of {2}) in {3} seconds.",
                            _innerProcess,
                            i + 1,
                            MaxRetryAttempts,
                            nextTry),
                        ex);

                    // Break the loop when the wait handle was signaled.
                    context.SleepOrThrow(nextTry);
                }
            }
        }

        private static LogLevel GetLogLevel(int i)
        {
            switch (i)
            {
                case 0:
                    return LogLevel.Debug;
                case 1:
                    return LogLevel.Info;
                case 2:
                    return LogLevel.Warn;
            }

            return LogLevel.Error;
        }

        public override string ToString()
        {
            return _innerProcess.ToString();
        }

        private TimeSpan GetBackOffMultiplier(int retryAttemptNumber)
        {
            //exponential/random retry back-off.
            var rand = new Random(Guid.NewGuid().GetHashCode());
            var nextTry = rand.Next(
                (int)Math.Pow(retryAttemptNumber, 2), (int)Math.Pow(retryAttemptNumber + 1, 2) + 1);

            return TimeSpan.FromSeconds(Math.Min(nextTry, MaxAttemptDelay.TotalSeconds));
        }
    }
}