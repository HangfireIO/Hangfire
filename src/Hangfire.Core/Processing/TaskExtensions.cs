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

using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.Logging;

namespace Hangfire.Processing
{
    internal static class TaskExtensions
    {
        private static readonly Type[] EmptyTypes = new Type[0];

        public static bool WaitOne([NotNull] this WaitHandle waitHandle, TimeSpan timeout, CancellationToken token)
        {
            if (waitHandle == null) throw new ArgumentNullException(nameof(waitHandle));
            if (timeout < Timeout.InfiniteTimeSpan) throw new ArgumentOutOfRangeException(nameof(timeout));

            token.ThrowIfCancellationRequested();

            using (var ev = token.GetCancellationEvent())
            {
                var waitHandles = new[] { waitHandle, ev.WaitHandle };

                var stopwatch = Stopwatch.StartNew();
                var waitResult = WaitHandle.WaitAny(waitHandles, timeout);
                stopwatch.Stop();

                var timeoutThreshold = TimeSpan.FromMilliseconds(1000);
                var elapsedThreshold = TimeSpan.FromMilliseconds(500);
                var protectionTime = TimeSpan.FromSeconds(1);

                if (waitResult == 0)
                {
                    return true;
                }

                if (!token.IsCancellationRequested &&
                    timeout >= timeoutThreshold &&
                    stopwatch.Elapsed < elapsedThreshold)
                {
                    try
                    {
                        var logger = LogProvider.GetLogger(typeof(TaskExtensions));
                        logger.Error($"Actual wait time for non-canceled token was '{stopwatch.Elapsed}' instead of '{timeout}', wait result: {waitResult}, using protective wait. Please report this to Hangfire developers.");
                    }
                    finally
                    {
                        Thread.Sleep(protectionTime);
                    }
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

            // We don't rely on RegisterWait's own timeout handling logic and passing
            // an infinite value, because I've seen a lot of evidences of unpredictable
            // behavior. In these cases WaitHandle wasn't signaled, but callback was
            // triggered too early without respecting the configured Timeout value. So
            // we are using Task.Delay instead to handle timeouts.
            var registration = ThreadPool.RegisterWaitForSingleObject(waitHandle, CallBack, tcs, Timeout.Infinite, executeOnlyOnce: true);

            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(token))
            {
                var result = await Task.WhenAny(tcs.Task, Task.Delay(timeout, cts.Token)).ConfigureAwait(false);
                registration.Unregister(null);

                if (result == tcs.Task)
                {
                    cts.Cancel();
                    return await tcs.Task.ConfigureAwait(false);
                }

                token.ThrowIfCancellationRequested();
                return false;
            }
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
            // I constantly get the following test fail on Windows and Linux on build machines. It happens not on every,
            // build so it looks like there's a race condition, but this happens too often. And it's unclear whether it's
            // a result of RegisterWaitForSingleObject or Timer's fault (since Task.Delay is based on timers). So if the
            // following test will fail on another assertion after we change "!timeout" to "true", then it's RWFSO's
            // fault, otherwise timers are wrong, because we can't get "false" here, because we don't register any timeouts
            // since the latest change.
            // [xUnit.net 00:00:27.6891962]     Hangfire.Core.Tests.Processing.TaskExtensionsFacts.WaitOneAsync_WaitsAndReturnsFalse_WhenNotSignaled_AndNonNullTimeout [FAIL]
            // X Hangfire.Core.Tests.Processing.TaskExtensionsFacts.WaitOneAsync_WaitsAndReturnsFalse_WhenNotSignaled_AndNonNullTimeout [96ms]
            // Error Message:
            // 00:00:00.0949474
            // Expected: True
            // Actual:   False
            //     Stack Trace:
            // at Hangfire.Core.Tests.Processing.TaskExtensionsFacts.WaitOneAsync_WaitsAndReturnsFalse_WhenNotSignaled_AndNonNullTimeout() in /home/travis/build/HangfireIO/Hangfire/tests/Hangfire.Core.Tests/Processing/TaskExtensionsFacts.cs:line 93
            // TODO: Change back to !timeout
            ((TaskCompletionSource<bool>)state).SetResult(/*!timedOut*/true);
        }

        private static TaskCompletionSource<T> CreateCompletionSource<T>()
        {
            return new TaskCompletionSource<T>(
#if !NET45
                TaskCreationOptions.RunContinuationsAsynchronously
#endif
            );
        }
    }
}