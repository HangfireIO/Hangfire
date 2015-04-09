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
using System.Threading;
using Hangfire.Annotations;
using Hangfire.Logging;

namespace Hangfire.Server
{
    internal class AutomaticRetryServerComponentWrapper : IServerComponent
    {
        private static readonly TimeSpan DefaultMaxAttemptDelay = TimeSpan.FromMinutes(5);
        private const int DefaultMaxRetryAttempts = int.MaxValue;

        private readonly IServerComponent _innerComponent;
        private readonly ILog _logger;

        public AutomaticRetryServerComponentWrapper([NotNull] IServerComponent innerComponent)
        {
            if (innerComponent == null) throw new ArgumentNullException("innerComponent");

            _innerComponent = innerComponent;
            _logger = LogProvider.GetLogger(_innerComponent.GetType());
            
            MaxRetryAttempts = DefaultMaxRetryAttempts;
            MaxAttemptDelay = DefaultMaxAttemptDelay;
            DelayCallback = GetBackOffMultiplier;
        }

        public int MaxRetryAttempts { get; set; }
        public TimeSpan MaxAttemptDelay { get; set; }
        public Func<int, TimeSpan> DelayCallback { get; set; } 

        public IServerComponent InnerComponent
        {
            get { return _innerComponent; }
        }

        public void Execute(CancellationToken cancellationToken)
        {
            ExecuteWithAutomaticRetry(cancellationToken);
        }

        private void ExecuteWithAutomaticRetry(CancellationToken cancellationToken)
        {
            for (var i = 0; i <= MaxRetryAttempts; i++)
            {
                try
                {
                    _innerComponent.Execute(cancellationToken);
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

                    _logger.ErrorException(
                        String.Format(
                            "Error occurred during execution of '{0}' component. Execution will be retried (attempt {1} of {2}) in {3} seconds.",
                            _innerComponent,
                            i + 1,
                            MaxRetryAttempts,
                            nextTry),
                        ex);

                    // Break the loop when the wait handle was signaled.
                    cancellationToken.WaitHandle.WaitOne(nextTry);
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
        }

        public override string ToString()
        {
            return _innerComponent.ToString();
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