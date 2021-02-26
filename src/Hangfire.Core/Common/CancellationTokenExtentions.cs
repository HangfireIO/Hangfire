// This file is part of Hangfire.
// Copyright © 2018 Sergey Odinokov.
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
        /// given cancellation token is canceled. The wait is performed on a dedicated event
        /// wait handle to avoid using the <see cref="CancellationToken.WaitHandle"/> property
        /// that may lead to high CPU issues.
        /// </summary>
        public static bool Wait(this CancellationToken cancellationToken, TimeSpan timeout)
        {
            using (var cancellationEvent = GetCancellationEvent(cancellationToken))
            {
                return cancellationEvent.WaitHandle.WaitOne(timeout);
            }
        }

        public class CancellationEvent : IDisposable
        {
            private static readonly Action<object> SetEventCallback = SetEvent;

            private readonly ManualResetEvent _mre;
            private CancellationTokenRegistration _registration;

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

            private static void SetEvent(object state)
            {
                try
                {
                    ((ManualResetEvent)state).Set();
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
