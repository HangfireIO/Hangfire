// This file is part of Hangfire. Copyright © 2017 Hangfire OÜ.
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
