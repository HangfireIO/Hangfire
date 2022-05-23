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
using System.Diagnostics;
using Hangfire.Logging;

namespace Hangfire.Profiling
{
    internal class SlowLogProfiler : IProfiler
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
            string message = null)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            var stopwatch = Stopwatch.StartNew();

            try
            {
                return action(instance);
            }
            finally
            {
                stopwatch.Stop();

                if (stopwatch.Elapsed >= _threshold)
                {
                    _logger.Warn($"Slow log: {instance?.ToString() ?? typeof(TInstance).ToString()} performed \"{message}\" in {(int)stopwatch.Elapsed.TotalSeconds} sec");
                }
            }
        }
    }
}