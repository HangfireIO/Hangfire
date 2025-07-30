// This file is part of Hangfire. Copyright © 2025 Hangfire OÜ.
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
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.Profiling;
using Hangfire.States;

namespace Hangfire
{
    public static class BackgroundConfigurationExtensions
    {
        public static IBackgroundConfiguration WithJobStorage(
            [NotNull] this IBackgroundConfiguration configuration,
            [NotNull] Func<IBackgroundConfiguration, JobStorage> callback)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            return configuration.With(callback);
        }

        public static IBackgroundConfiguration WithJobFactory(
            [NotNull] this IBackgroundConfiguration configuration,
            [NotNull] Func<IBackgroundConfiguration, IBackgroundJobFactory> callback)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            return configuration.With(callback);
        }

        public static IBackgroundConfiguration WithStateMachine(
            [NotNull] this IBackgroundConfiguration configuration,
            [NotNull] Func<IBackgroundConfiguration, IStateMachine> callback)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            return configuration.With(callback);
        }

        public static IBackgroundConfiguration WithStateChanger(
            [NotNull] this IBackgroundConfiguration configuration,
            [NotNull] Func<IBackgroundConfiguration, IBackgroundJobStateChanger> callback)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            return configuration.With(callback);
        }

        public static IBackgroundConfiguration WithJobFilterProvider(
            [NotNull] this IBackgroundConfiguration configuration,
            [NotNull] Func<IBackgroundConfiguration, IJobFilterProvider> callback)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            return configuration.With(callback);
        }

        public static IBackgroundConfiguration WithTimeZoneResolver(
            [NotNull] this IBackgroundConfiguration configuration,
            [NotNull] Func<IBackgroundConfiguration, ITimeZoneResolver> callback)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            return configuration.With(callback);
        }

        public static IBackgroundConfiguration WithClock(
            [NotNull] this IBackgroundConfiguration configuration,
            [NotNull] Func<IBackgroundConfiguration, IBackgroundClock> callback)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            return configuration.With(callback);
        }

        internal static IBackgroundConfiguration WithProfiler(
            [NotNull] this IBackgroundConfiguration configuration,
            [NotNull] Func<IBackgroundConfiguration, IProfiler> callback)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            return configuration.With(callback);
        }
    }
}