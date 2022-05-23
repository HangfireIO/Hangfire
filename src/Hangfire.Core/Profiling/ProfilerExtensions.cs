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