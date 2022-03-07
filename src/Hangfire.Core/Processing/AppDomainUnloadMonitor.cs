// This file is part of Hangfire. Copyright © 2017 Sergey Odinokov.
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

#if !NETSTANDARD1_3
using System;
using System.Threading;

namespace Hangfire.Processing
{
    internal static class AppDomainUnloadMonitor
    {
        private static int _initialized;
        private static bool _isUnloading;

        public static void EnsureInitialized()
        {
            if (Interlocked.CompareExchange(ref _initialized, 1, 0) == 0)
            {
                AppDomain.CurrentDomain.DomainUnload += OnDomainUnload;
                AppDomain.CurrentDomain.ProcessExit += OnDomainUnload;
            }
        }

        public static bool IsUnloading => Volatile.Read(ref _isUnloading);

        private static void OnDomainUnload(object sender, EventArgs args)
        {
            Volatile.Write(ref _isUnloading, true);
        }
    }
}
#endif
