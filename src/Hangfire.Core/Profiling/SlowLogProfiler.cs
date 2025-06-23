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
using Hangfire.Annotations;
using Hangfire.Logging;

#nullable enable

namespace Hangfire.Profiling
{
    internal sealed class SlowLogProfiler : IProfiler
    {
        private static readonly TimeSpan DefaultThreshold = TimeSpan.FromMinutes(1);

        private readonly int _thresholdMs;
        private readonly ILog _logger;

        public SlowLogProfiler(ILog logger)
            : this(logger, DefaultThreshold)
        {
        }

        public SlowLogProfiler(ILog logger, TimeSpan threshold)
        {
            _thresholdMs = (int)threshold.TotalMilliseconds;
            _logger = logger;
        }

        public TResult InvokeMeasured<TInstance, TResult>(
            TInstance? instance,
            [InstantHandle] Func<TInstance?, TResult> action,
            [InstantHandle] Func<TInstance?, string>? messageFunc = null)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            var started = Environment.TickCount;

            try
            {
                return action(instance);
            }
            finally
            {
                var elapsed = unchecked(Environment.TickCount - started); 
                if (elapsed >= _thresholdMs)
                {
                    _logger.Warn($"Slow log: {instance?.ToString() ?? typeof(TInstance).ToString()} performed \"{messageFunc?.Invoke(instance)}\" in {elapsed / 1000} sec");
                }
            }
        }
    }
}