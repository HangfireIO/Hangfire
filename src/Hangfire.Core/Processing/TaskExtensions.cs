// This file is part of Hangfire.
// Copyright © 2017 Sergey Odinokov.
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
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Hangfire.Annotations;
using Hangfire.Common;

namespace Hangfire.Processing
{
    internal static class TaskExtensions
    {
        private static readonly Type[] EmptyTypes = new Type[0];
        private static readonly WaitHandle InvalidWaitHandleInstance = new InvalidWaitHandle();

        public static bool WaitOne([NotNull] this WaitHandle waitHandle, TimeSpan timeout, CancellationToken token)
        {
            if (waitHandle == null) throw new ArgumentNullException(nameof(waitHandle));
            if (timeout < Timeout.InfiniteTimeSpan) throw new ArgumentOutOfRangeException(nameof(timeout));

            token.ThrowIfCancellationRequested();

            using (var ev = token.GetCancellationEvent())
            {
                var waitHandles = new[] { waitHandle, ev.WaitHandle };
                var waitResult = WaitHandle.WaitAny(waitHandles, timeout);

                if (waitResult == 0)
                {
                    return true;
                }

                token.ThrowIfCancellationRequested();
                return false;
            }
        }

        public static async Task<bool> WaitOneAsync([NotNull] this WaitHandle waitHandle, TimeSpan timeout, CancellationToken token)
        {
            if (waitHandle == null) throw new ArgumentNullException(nameof(waitHandle));
            if (timeout < Timeout.InfiniteTimeSpan) throw new ArgumentOutOfRangeException(nameof(timeout));

            token.ThrowIfCancellationRequested();

            if (waitHandle.WaitOne(TimeSpan.Zero))
            {
                return true;
            }

            var tcs = CreateCompletionSource<bool>();
            var registration = ThreadPool.RegisterWaitForSingleObject(waitHandle, CallBack, tcs, timeout, executeOnlyOnce: true);

            if (token.CanBeCanceled)
            {
                token.Register(Callback, Tuple.Create(registration, tcs, token), useSynchronizationContext: false);
            }

            return await tcs.Task.ConfigureAwait(false);
        }

        public static bool IsTaskLike(this Type type, out Func<object, Task> getTaskFunc)
        {
            var typeInfo = type.GetTypeInfo();

            // There are no primitive types that behave as Task
            if (!typeInfo.IsPrimitive)
            {
                if (typeof(Task).GetTypeInfo().IsAssignableFrom(typeInfo))
                {
                    getTaskFunc = obj => (Task)obj;
                    return true;
                }

                // We are don't relying on GetAwaiter/GetResult methods for ValueTask,
                // because it's not a valid pattern to use them as written here:
                // https://devblogs.microsoft.com/dotnet/understanding-the-whys-whats-and-whens-of-valuetask/#user-content-valid-consumption-patterns-for-valuetasks
                // So we're using their `AsTask` method to create a task first, and then
                // waiting on it.
                // This method can be replaced with wrapping in a custom awaiter like
                // in ASP.NET Core, but this step requires using the `await` keyword
                // to get the result, so can be implemented only in future.
                if (typeInfo.FullName != null &&
                    typeInfo.FullName.StartsWith("System.Threading.Tasks.ValueTask", StringComparison.Ordinal))
                {
                    var asTask = type.GetRuntimeMethod("AsTask", EmptyTypes);

                    if (asTask != null && asTask.IsPublic && !asTask.IsStatic &&
                        typeof(Task).GetTypeInfo().IsAssignableFrom(asTask.ReturnType.GetTypeInfo()))
                    {
                        getTaskFunc = obj => (Task) asTask.Invoke(obj, null);
                        return true;
                    }
                }
            }

            getTaskFunc = null;
            return false;
        }

        public static object GetTaskLikeResult([NotNull] this Task task, object obj, Type returnType)
        {
            if (task == null) throw new ArgumentNullException(nameof(task));

            if (task != obj)
            {
                // We shouldn't call GetAwaiter/GetResult on ValueTask directly, because
                // there may be a race condition as tells us this article:
                // https://devblogs.microsoft.com/dotnet/understanding-the-whys-whats-and-whens-of-valuetask/#user-content-valid-consumption-patterns-for-valuetasks
                // So we are waiting on task, returned by the AsTask method to ensure it's
                // completed, before querying for the result.
                task.GetAwaiter().GetResult();
            }

            // ReturnType is used instead of task.GetType, because we should return `null` result,
            // when method is returning non-generic Task. However async state machines may use
            // Task<VoidTaskResult> or Task<VoidResult> for these cases, and the number of such
            // void types is pretty high. So it's much safer to call GetAwaiter/GetResult on an
            // original result object.

            // Awaitable type must have a public parameterless GetAwaiter instance method, ...
            var getAwaiter = returnType.GetRuntimeMethod("GetAwaiter", EmptyTypes);
            if (getAwaiter == null || getAwaiter.IsStatic || !getAwaiter.IsPublic) return null;

            var awaiterType = getAwaiter.ReturnType;

            // ... and also have a public parameterless GetResult instance method
            var getResult = awaiterType.GetRuntimeMethod("GetResult", EmptyTypes);
            if (getResult == null || getResult.IsStatic || !getResult.IsPublic) return null;

            var awaiter = getAwaiter.Invoke(obj, null);
            return getResult.Invoke(awaiter, null);
        }

        private static void CallBack(object state, bool timedOut)
        {
            // We do call the Unregister method to prevent race condition between
            // registered wait and cancellation token registration, so can use the
            // SetResult safely.
            ((TaskCompletionSource<bool>)state).SetResult(!timedOut);
        }

        private static void Callback(object state)
        {
            // We need to ensure there's no race condition, where wait handle was
            // set, but callback wasn't fully completed. In this case handle is
            // acquired, but task is cancelled.
            var ctx = (Tuple<RegisteredWaitHandle, TaskCompletionSource<bool>, CancellationToken>)state;

            ctx.Item1.Unregister(InvalidWaitHandleInstance);
            TrySetCanceled(ctx.Item2, ctx.Item3);
        }

        private static TaskCompletionSource<T> CreateCompletionSource<T>()
        {
            return new TaskCompletionSource<T>(
#if !NET45
                TaskCreationOptions.RunContinuationsAsynchronously
#endif
            );
        }

        private static void TrySetCanceled<T>(TaskCompletionSource<T> source, CancellationToken token)
        {
            source.TrySetCanceled(
#if !NET45
                token
#endif
            );
        }

        private sealed class InvalidWaitHandle : WaitHandle
        {
#if !NETSTANDARD1_3
            [Obsolete("Use the SafeWaitHandle property instead.")]
            public override IntPtr Handle
            {
                get { return InvalidHandle; }
                set { throw new InvalidOperationException(); }
            }
#endif
        }
    }
}