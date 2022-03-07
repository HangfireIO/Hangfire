// This file is part of Hangfire. Copyright © 2018 Sergey Odinokov.
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
