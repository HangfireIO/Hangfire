// This file is part of Hangfire. Copyright © 2019 Hangfire OÜ.
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
using Hangfire.Logging;

namespace Hangfire.Profiling
{
    internal sealed class SlowLogProfiler : IProfiler
    {
        private static readonly TimeSpan DefaultThreshold = TimeSpan.FromMinutes(1);

        private readonly TimeSpan _threshold;
        private readonly ILog _logger;

        public SlowLogProfiler(ILog logger)
            : this(logger, DefaultThreshold)
        {
        }

        public SlowLogProfiler(ILog logger, TimeSpan threshold)
        {
            _threshold = threshold;
            _logger = logger;
        }

        public TResult InvokeMeasured<TInstance, TResult>(
            TInstance instance,
            Func<TInstance, TResult> action,
            Func<TInstance, string> messageFunc = null)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            var startedAt = Environment.TickCount;

            // TODO: Change implementation to thread-based, once it is possible to query custom services everywhere
            using (new Timer(LogWarningMessage, null, _threshold, _threshold))
            {
                return action(instance);
            }

            void LogWarningMessage(object state)
            {
                var elapsedSec = unchecked(Environment.TickCount - startedAt) / 1_000;

                var type = instance?.ToString() ?? typeof(TInstance).ToString();
                var message = messageFunc?.Invoke(instance) ?? "(null)";

                _logger.Warn($"Slow log: {type} is still performing \"{message}\" after {elapsedSec} sec");
            }
        }
    }
}