// This file is part of Hangfire. Copyright © 2016 Hangfire OÜ.
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
using Hangfire.States;

namespace Hangfire
{
    public class RecurringJobOptions
    {
        private TimeZoneInfo _timeZone;
        private string _queueName;

        public RecurringJobOptions()
        {
            _timeZone = TimeZoneInfo.Utc;
            _queueName = EnqueuedState.DefaultQueue;
            MisfireHandling = MisfireHandlingMode.Relaxed;
        }

        [NotNull]
        public TimeZoneInfo TimeZone
        {
            get => _timeZone;
            set => _timeZone = value ?? throw new ArgumentNullException(nameof(value));
        }

        [Obsolete("Please use non-obsolete AddOrUpdate with the explicit `queue` parameter instead. Will be removed in 2.0.0.")]
        [NotNull]
        public string QueueName
        {
            get => _queueName;
            set
            {
                EnqueuedState.ValidateQueueName(nameof(value), value);
                _queueName = value;
            }
        }

        public MisfireHandlingMode MisfireHandling { get; set; }
    }
}
