// This file is part of Hangfire. Copyright © 2017 Hangfire OÜ.
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

        public static bool IsUnloading => Volatile.Read(ref _isUnloading) || Server.AspNetShutdownDetector.DisposingHttpRuntime;

        private static void OnDomainUnload(object sender, EventArgs args)
        {
            Volatile.Write(ref _isUnloading, true);
        }
    }
}
#endif
