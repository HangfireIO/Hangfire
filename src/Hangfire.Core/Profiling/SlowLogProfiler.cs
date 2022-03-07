// This file is part of Hangfire. Copyright © 2019 Sergey Odinokov.
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