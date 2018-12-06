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
using System.Threading;
using System.Threading.Tasks;
using Hangfire.Annotations;

namespace Hangfire.Processing
{
    internal static class TaskExtensions
    {
#if !NETSTANDARD1_3
        internal static readonly WaitHandle InvalidWaitHandleInstance = new InvalidWaitHandle();

        public static Task<bool> AsTask([NotNull] this WaitHandle waitHandle, CancellationToken token)
        {
            return AsTask(waitHandle, token, Timeout.InfiniteTimeSpan);
        }

        public static async Task<bool> AsTask([NotNull] this WaitHandle waitHandle, CancellationToken token, TimeSpan timeout)
        {
            if (waitHandle == null) throw new ArgumentNullException(nameof(waitHandle));
            if (timeout < Timeout.InfiniteTimeSpan) throw new ArgumentOutOfRangeException(nameof(timeout));

            token.ThrowIfCancellationRequested();

            var tcs = CreateCompletionSource<bool>();
            var registration = ThreadPool.RegisterWaitForSingleObject(waitHandle, CallBack, tcs, timeout, executeOnlyOnce: true);

            if (token.CanBeCanceled)
            {
                token.Register(Callback, Tuple.Create(registration, tcs, token), useSynchronizationContext: false);
            }

            return await tcs.Task;
        }
#endif

        public static async Task AsTask(this CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var tcs = CreateCompletionSource<object>();

            if (cancellationToken.CanBeCanceled)
            {
                cancellationToken.Register(
                    Action,
                    Tuple.Create(tcs, cancellationToken),
                    useSynchronizationContext: false);
            }

            await tcs.Task.ConfigureAwait(false);
        }

#if !NETSTANDARD1_3
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
#endif

        private static void Action(object state)
        {
            var ctx = (Tuple<TaskCompletionSource<object>, CancellationToken>)state;
            TrySetCanceled(ctx.Item1, ctx.Item2);
        }
        
        private static TaskCompletionSource<T> CreateCompletionSource<T>()
        {
            return new TaskCompletionSource<T>(
#if NET46
                TaskCreationOptions.RunContinuationsAsynchronously
#endif
            );
        }

        private static void TrySetCanceled<T>(TaskCompletionSource<T> source, CancellationToken token)
        {
            source.TrySetCanceled(
#if NET46
                token
#endif
                );
        }

#if !NETSTANDARD1_3
        private sealed class InvalidWaitHandle : WaitHandle
        {
            [Obsolete("Use the SafeWaitHandle property instead.")]
            public override IntPtr Handle
            {
                get { return InvalidHandle; }
                set { throw new InvalidOperationException(); }
            }
        }
#endif
    }
}