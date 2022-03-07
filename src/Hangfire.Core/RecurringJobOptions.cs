// This file is part of Hangfire. Copyright © 2016 Sergey Odinokov.
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
