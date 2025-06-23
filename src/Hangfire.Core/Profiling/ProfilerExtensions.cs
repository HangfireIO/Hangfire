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

#nullable enable

namespace Hangfire.Profiling
{
    internal static class ProfilerExtensions
    {
        public static void InvokeMeasured<TInstance>(
            this IProfiler profiler,
            TInstance? instance, 
            [InstantHandle] Action<TInstance?> action,
            [InstantHandle] Func<TInstance?, string>? messageFunc = null)
        {
            if (profiler == null) throw new ArgumentNullException(nameof(profiler));
            if (action == null) throw new ArgumentNullException(nameof(action));

            var instanceAction = new InstanceAction<TInstance?>(instance, action, messageFunc);
            profiler.InvokeMeasured(instanceAction, InvokeAction, instanceAction.MessageFunc != null ? MessageCallback : null);
        }

        private static bool InvokeAction<TInstance>(InstanceAction<TInstance?> tuple)
        {
            tuple.Action(tuple.Instance);
            return true;
        }

        private static string MessageCallback<TInstance>(InstanceAction<TInstance?> action)
        {
            return action.MessageFunc!.Invoke(action.Instance);
        }

        private readonly struct InstanceAction<TInstance>
        {
            public InstanceAction(TInstance? instance, Action<TInstance?> action, Func<TInstance?, string>? messageFunc)
            {
                Instance = instance;
                Action = action ?? throw new ArgumentNullException(nameof(action));
                MessageFunc = messageFunc;
            }

            public TInstance? Instance { get; }
            public Action<TInstance> Action { get; }
            public Func<TInstance, string>? MessageFunc { get; }

            public override string ToString()
            {
                return Instance?.ToString() ?? typeof(TInstance).ToString();
            }
        }
    }
}