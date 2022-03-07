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
using Hangfire.Logging;
using Microsoft.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Hangfire.AspNetCore
{
    internal class AspNetCoreLog : ILog
    {
        private static readonly Func<object, Exception, string> MessageFormatterFunc = MessageFormatter;

        private readonly ILogger _targetLogger;

        public AspNetCoreLog([NotNull] ILogger targetLogger)
        {
            if (targetLogger == null) throw new ArgumentNullException(nameof(targetLogger));
            _targetLogger = targetLogger;
        }

        public bool Log(Logging.LogLevel logLevel, Func<string> messageFunc, Exception exception = null)
        {
            var targetLogLevel = ToTargetLogLevel(logLevel);

            // When messageFunc is null, Hangfire.Logging
            // just determines is logging enabled.
            if (messageFunc == null)
            {
                return _targetLogger.IsEnabled(targetLogLevel);
            }

            _targetLogger.Log(targetLogLevel, 0, messageFunc(), exception, MessageFormatterFunc);
            return true;
        }

        private static LogLevel ToTargetLogLevel(Logging.LogLevel logLevel)
        {
            switch (logLevel)
            {
                case Logging.LogLevel.Trace:
                    return LogLevel.Trace;
                case Logging.LogLevel.Debug:
                    return LogLevel.Debug;
                case Logging.LogLevel.Info:
                    return LogLevel.Information;
                case Logging.LogLevel.Warn:
                    return LogLevel.Warning;
                case Logging.LogLevel.Error:
                    return LogLevel.Error;
                case Logging.LogLevel.Fatal:
                    return LogLevel.Critical;
            }

            return LogLevel.None;
        }

        private static string MessageFormatter(object state, Exception exception)
        {
            return state.ToString();
        }
    }
}
