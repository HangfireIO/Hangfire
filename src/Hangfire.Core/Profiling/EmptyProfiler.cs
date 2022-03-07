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

namespace Hangfire.Profiling
{
    internal sealed class EmptyProfiler : IProfiler
    {
        internal static readonly IProfiler Instance = new EmptyProfiler();

        private EmptyProfiler()
        {
        }

        public TResult InvokeMeasured<TInstance, TResult>(
            TInstance instance, 
            Func<TInstance, TResult> action,
            string message)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            return action(instance);
        }
    }
}