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
        private const int DefaultMaxRetryAttempts = 10;

        private readonly IServerComponent _innerComponent;
        private readonly int _maxRetryAttempts;
        private readonly Func<int, TimeSpan> _delayCallback;
        private readonly ILog _logger;

        public AutomaticRetryServerComponentWrapper([NotNull] IServerComponent innerComponent)
            : this(innerComponent, DefaultMaxRetryAttempts)
        {
        }

        public AutomaticRetryServerComponentWrapper(
            [NotNull] IServerComponent innerComponent,
            int maxRetryAttempts)
            : this(innerComponent, maxRetryAttempts, GetBackOffMultiplier)
        {
        }

        public AutomaticRetryServerComponentWrapper(
            [NotNull] IServerComponent innerComponent,
            int maxRetryAttempts, 
            [NotNull] Func<int, TimeSpan> delayCallback)
        {
            if (innerComponent == null) throw new ArgumentNullException("innerComponent");
            if (delayCallback == null) throw new ArgumentNullException("delayCallback");
            if (maxRetryAttempts < 0)
            {
                throw new ArgumentOutOfRangeException(
                    "maxRetryAttempts",
                    "MaxRetryAttempts property value must be greater or equal to 0.");
            }

            _innerComponent = innerComponent;
            _maxRetryAttempts = maxRetryAttempts;
            _delayCallback = delayCallback;
            _logger = LogProvider.GetLogger(_innerComponent.GetType());
        }

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
            for (var i = 0; i <= _maxRetryAttempts; i++)
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
                    if (i >= _maxRetryAttempts - 1) throw;

                    var nextTry = _delayCallback(i);

                    _logger.ErrorFormat(
                        "Error occurred during execution of '{0}' component. Execution will be retried (attempt {1} of {2}) in {3} seconds.",
                        ex,
                        _maxRetryAttempts,
                        i + 1,
                        _maxRetryAttempts,
                        nextTry);

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

        private static TimeSpan GetBackOffMultiplier(int retryAttemptNumber)
        {
            //exponential/random retry back-off.
            var rand = new Random(Guid.NewGuid().GetHashCode());
            var nextTry = rand.Next(
                (int)Math.Pow(retryAttemptNumber, 2), (int)Math.Pow(retryAttemptNumber + 1, 2) + 1);

            return TimeSpan.FromSeconds(nextTry);
        }
    }
}