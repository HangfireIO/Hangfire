// This file is part of Hangfire. Copyright © 2018 Hangfire OÜ.
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
using System.Diagnostics;
using System.Threading;
using Hangfire.Logging;

namespace Hangfire.Common
{
    public static class CancellationTokenExtentions
    {
        /// <summary>
        /// Returns a class that contains a <see cref="EventWaitHandle"/> that is set, when
        /// the given <paramref name="cancellationToken"/> is canceled. This method is based
        /// on cancellation token registration and avoids using the <see cref="CancellationToken.WaitHandle"/>
        /// property as it may lead to high CPU issues.
        /// </summary>
        public static CancellationEvent GetCancellationEvent(this CancellationToken cancellationToken)
        {
            return new CancellationEvent(cancellationToken);
        }

        /// <summary>
        /// Performs a wait until the specified <paramref name="timeout"/> is elapsed or the
        /// given cancellation token is canceled and throw <see cref="OperationCanceledException"/>
        /// exception if wait succeeded. The wait is performed on a dedicated event
        /// wait handle to avoid using the <see cref="CancellationToken.WaitHandle"/> property
        /// that may lead to high CPU issues.
        /// </summary>
        [Obsolete("Please use overload with `ILog` parameter instead. Will be removed in 2.0.0.")]
        public static void WaitOrThrow(this CancellationToken cancellationToken, TimeSpan timeout)
        {
            WaitOrThrow(cancellationToken, LogProvider.GetLogger(typeof(CancellationTokenExtentions)), timeout);
        }

        /// <summary>
        /// Performs a wait until the specified <paramref name="timeout"/> is elapsed or the
        /// given cancellation token is canceled and throw <see cref="OperationCanceledException"/>
        /// exception if wait succeeded. The wait is performed on a dedicated event
        /// wait handle to avoid using the <see cref="CancellationToken.WaitHandle"/> property
        /// that may lead to high CPU issues.
        /// </summary>
        public static void WaitOrThrow(this CancellationToken cancellationToken, ILog logger, TimeSpan timeout)
        {
            if (Wait(cancellationToken, logger, timeout))
            {
                throw new OperationCanceledException(cancellationToken);
            }
        }

        /// <summary>
        /// Performs a wait until the specified <paramref name="timeout"/> is elapsed or the
        /// given cancellation token is canceled. The wait is performed on a dedicated event
        /// wait handle to avoid using the <see cref="CancellationToken.WaitHandle"/> property
        /// that may lead to high CPU issues.
        /// </summary>
        [Obsolete("Please use the overload with `ILog` parameter instead. Will be removed in 2.0.0.")]
        public static bool Wait(this CancellationToken cancellationToken, TimeSpan timeout)
        {
            return Wait(cancellationToken, LogProvider.GetLogger(typeof(CancellationTokenExtentions)), timeout);
        }

        /// <summary>
        /// Performs a wait until the specified <paramref name="timeout"/> is elapsed or the
        /// given cancellation token is canceled. The wait is performed on a dedicated event
        /// wait handle to avoid using the <see cref="CancellationToken.WaitHandle"/> property
        /// that may lead to high CPU issues.
        /// </summary>
        public static bool Wait(this CancellationToken cancellationToken, ILog logger, TimeSpan timeout)
        {
            using var cancellationEvent = GetCancellationEvent(cancellationToken);

            var stopwatch = Stopwatch.StartNew();
            var waitResult = cancellationEvent.WaitHandle.WaitOne(timeout);
            stopwatch.Stop();

            var timeoutThreshold = TimeSpan.FromMilliseconds(1000);
            var elapsedThreshold = TimeSpan.FromMilliseconds(500);
            var protectionTime = TimeSpan.FromSeconds(1);

            // There was a precedent of the following message logged when CancellationToken.WaitHandle
            // was used directly, instead of having our custom CancellationEvent class used, please see
            // https://github.com/HangfireIO/Hangfire/issues/2447
            if (!cancellationToken.IsCancellationRequested &&
                timeout >= timeoutThreshold &&
                stopwatch.Elapsed < elapsedThreshold)
            {
                try
                {
                    logger.Error($"Actual wait time for non-canceled token was '{stopwatch.Elapsed.TotalMilliseconds}' ms instead of '{timeout.TotalMilliseconds}' ms, wait result: {waitResult}, using protective wait. Please report this to Hangfire developers.");
                }
                finally
                {
                    Thread.Sleep(protectionTime);
                }
            }

            return waitResult;
        }

        public sealed class CancellationEvent : IDisposable
        {
            private static readonly Action<object?> SetEventCallback = SetEvent;

            private readonly ManualResetEvent _mre;
            private CancellationTokenRegistration _registration; // Do not make read-only as this type is not read-only in .NET Framework and some other platforms

            public CancellationEvent(CancellationToken cancellationToken)
            {
                _mre = new ManualResetEvent(false);
                _registration = cancellationToken.Register(SetEventCallback, _mre);
            }

            public EventWaitHandle WaitHandle => _mre;

            public void Dispose()
            {
                _registration.Dispose();
                _mre.Dispose();
            }

            private static void SetEvent(object? state)
            {
                try
                {
                    if (state is not ManualResetEvent ev)
                    {
                        throw new InvalidOperationException("Invalid state object type: " + state?.GetType().FullName);
                    }
                    ev.Set();
                }
                catch (ObjectDisposedException)
                {
                    // When our event instance is already disposed, we already
                    // aren't interested in any notifications. This statement
                    // is just to ensure we don't throw any exceptions.
                }
            }
        }
    }
}
