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
using Hangfire.Annotations;

namespace Hangfire.Profiling
{
    internal static class ProfilerExtensions
    {
        public static void InvokeMeasured<TInstance>(
            [NotNull] this IProfiler profiler,
            [CanBeNull] TInstance instance, 
            [NotNull] Action<TInstance> action,
            [CanBeNull] string message = null)
        {
            if (profiler == null) throw new ArgumentNullException(nameof(profiler));
            if (action == null) throw new ArgumentNullException(nameof(action));

            profiler.InvokeMeasured(new InstanceAction<TInstance>(instance, action), InvokeAction, message);
        }

        private static bool InvokeAction<TInstance>(InstanceAction<TInstance> tuple)
        {
            tuple.Action(tuple.Instance);
            return true;
        }

        private struct InstanceAction<TInstance>
        {
            public InstanceAction([CanBeNull] TInstance instance, [NotNull] Action<TInstance> action)
            {
                if (action == null) throw new ArgumentNullException(nameof(action));

                Instance = instance;
                Action = action;
            }

            [CanBeNull]
            public TInstance Instance { get; }

            [NotNull]
            public Action<TInstance> Action { get; }

            public override string ToString()
            {
                return Instance?.ToString() ?? typeof(TInstance).ToString();
            }
        }
    }
}