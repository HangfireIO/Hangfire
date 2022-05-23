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
            TimeZone = TimeZoneInfo.Utc;
            QueueName = EnqueuedState.DefaultQueue;
        }

        [NotNull]
        public TimeZoneInfo TimeZone
        {
            get { return _timeZone; }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));

                _timeZone = value;
            }
        }

        [NotNull]
        public string QueueName
        {
            get { return _queueName; }
            set
            {
                EnqueuedState.ValidateQueueName(nameof(value), value);
                _queueName = value;
            }
        }
    }
}
