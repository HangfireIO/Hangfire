// This file is part of Hangfire.
// Copyright © 2015 Sergey Odinokov.
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

namespace Hangfire.Server
{
    public static class BackgroundProcessContextExtensions
    {
        public static bool Sleep([NotNull] this BackgroundProcessContext context, TimeSpan timeout)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            return context.CancellationToken.WaitHandle.WaitOne(timeout);
        }

        public static void SleepOrThrow([NotNull] this BackgroundProcessContext context, TimeSpan timeout)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            context.CancellationToken.WaitHandle.WaitOne(timeout);
            context.CancellationToken.ThrowIfCancellationRequested();
        }
    }
}