// This file is part of Hangfire. Copyright © 2017 Sergey Odinokov.
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

namespace Hangfire.Processing
{
    internal sealed class BackgroundExecutionOptions
    {
        private static readonly TimeSpan DefaultMaxAttemptDelay = TimeSpan.FromMinutes(5);
        private TimeSpan _warningThreshold;
        private TimeSpan _errorThreshold;
        private TimeSpan _stillErrorThreshold;
        private Func<int, TimeSpan> _retryDelay;

        public BackgroundExecutionOptions()
        {
            WarningThreshold = TimeSpan.FromSeconds(5);
            ErrorThreshold = TimeSpan.FromSeconds(15);
            StillErrorThreshold = TimeSpan.FromSeconds(60);
            RetryDelay = GetBackOffMultiplier;
        }

        public string Name { get; set; }

        public TimeSpan WarningThreshold
        {
            get { return _warningThreshold; }
            set
            {
                if (value < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(value), "Value should be greater than or equal to TimeSpan.Zero");
                _warningThreshold = value;
            }
        }

        public TimeSpan ErrorThreshold
        {
            get { return _errorThreshold; }
            set
            {
                if (value < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(value), "Value should be greater than or equal to TimeSpan.Zero");
                _errorThreshold = value;
            }
        }

        public TimeSpan StillErrorThreshold
        {
            get { return _stillErrorThreshold; }
            set
            {
                if (value < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(value), "Value should be greater than or equal to TimeSpan.Zero");
                _stillErrorThreshold = value;
            }
        }

        public Func<int, TimeSpan> RetryDelay
        {
            get { return _retryDelay; }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                _retryDelay = value;
            }
        }

        internal static TimeSpan GetBackOffMultiplier(int retryAttemptNumber)
        {
            //exponential/random retry back-off.
            var rand = new Random(Guid.NewGuid().GetHashCode());
            var nextTry = rand.Next(
                (int)Math.Pow(retryAttemptNumber, 2), (int)Math.Pow(retryAttemptNumber, 2) + 1);

            return TimeSpan.FromSeconds(Math.Min(nextTry, DefaultMaxAttemptDelay.TotalSeconds));
        }
    }
}
