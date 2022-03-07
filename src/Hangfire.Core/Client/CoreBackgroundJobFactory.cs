// This file is part of Hangfire. Copyright © 2013-2014 Sergey Odinokov.
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
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.Logging;
using Hangfire.States;

namespace Hangfire.Client
{
    internal class CoreBackgroundJobFactory : IBackgroundJobFactory
    {
        private readonly ILog _logger = LogProvider.GetLogger(typeof(CoreBackgroundJobFactory));
        private readonly object _syncRoot = new object();
        private int _retryAttempts;
        private Func<int, TimeSpan> _retryDelayFunc;

        public CoreBackgroundJobFactory([NotNull] IStateMachine stateMachine)
        {
            StateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
            RetryAttempts = 0;
            RetryDelayFunc = GetRetryDelay;
        }

        public IStateMachine StateMachine { get; }

        public int RetryAttempts
        {
            get { lock (_syncRoot) { return _retryAttempts; } }
            set { lock (_syncRoot) { _retryAttempts = value; } }
        }

        public Func<int, TimeSpan> RetryDelayFunc
        {
            get { lock (_syncRoot) { return _retryDelayFunc; } }
            set { lock (_syncRoot) { _retryDelayFunc = value; } }
        }

        public BackgroundJob Create(CreateContext context)
        {
            var attemptsLeft = Math.Max(RetryAttempts, 0);
            var parameters = context.Parameters.ToDictionary(
                x => x.Key,
                x => SerializationHelper.Serialize(x.Value, SerializationOption.User));

            var createdAt = DateTime.UtcNow;

            // Retry may cause multiple background jobs to be created, especially when there's
            // a timeout-related exception. But initialization attempt will be performed only
            // for the most recent job, leaving all the previous ones in a non-initialized state
            // and making them invisible to other parts of the system, since no one knows their
            // identifiers. Since they also will be eventually expired leaving no trace, we can
            // consider that only one background job is created, regardless of retry attempts
            // number.
            var jobId = RetryOnException(ref attemptsLeft, _ => context.Connection.CreateExpiredJob(
                context.Job,
                parameters,
                createdAt,
                TimeSpan.FromDays(30)));

            if (String.IsNullOrEmpty(jobId))
            {
                return null;
            }

            var backgroundJob = new BackgroundJob(jobId, context.Job, createdAt);

            if (context.InitialState != null)
            {
                RetryOnException(ref attemptsLeft, attempt =>
                {
                    if (attempt > 0)
                    {
                        // Normally, a distributed lock should be applied when making a retry, since
                        // it's possible to get a timeout exception, when transaction was actually
                        // committed. But since background job can't be returned to a position where
                        // its state is null, and since only the current thread knows the job's identifier
                        // when its state is null, and since we shouldn't do anything when it's non-null,
                        // there will be no any race conditions.
                        var data = context.Connection.GetJobData(jobId);
                        if (data == null) throw new InvalidOperationException($"Was unable to initialize a background job '{jobId}', because it doesn't exists.");

                        if (!String.IsNullOrEmpty(data.State)) return;
                    }

                    using (var transaction = context.Connection.CreateWriteTransaction())
                    {
                        var applyContext = new ApplyStateContext(
                            context.Storage,
                            context.Connection,
                            transaction,
                            backgroundJob,
                            context.InitialState,
                            oldStateName: null,
                            profiler: context.Profiler);

                        StateMachine.ApplyState(applyContext);

                        transaction.Commit();
                    }
                });
            }

            return backgroundJob;
        }

        private void RetryOnException(ref int attemptsLeft, Action<int> action)
        {
            RetryOnException(ref attemptsLeft, attempt =>
            {
                action(attempt);
                return true;
            });
        }

        private T RetryOnException<T>(ref int attemptsLeft, Func<int, T> action)
        {
            var exceptions = new List<Exception>();
            var attempt = 0;
            var delay = TimeSpan.Zero;

            do
            {
                try
                {
                    if (delay > TimeSpan.Zero)
                    {
                        Thread.Sleep(delay);
                    }

                    return action(attempt++);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                    _logger.DebugException("An exception occurred while creating a background job, see inner exception for details.", ex);
                    delay = RetryDelayFunc(attempt);
                }
            } while (attemptsLeft-- > 0);

            if (exceptions.Count == 1)
            {
                ExceptionDispatchInfo.Capture(exceptions[0]).Throw();
            }

            throw new AggregateException(exceptions);
        }

        private static TimeSpan GetRetryDelay(int retryAttempt)
        {
            switch (retryAttempt)
            {
                case 1: return TimeSpan.Zero;
                case 2: return TimeSpan.FromMilliseconds(50);
                case 3: return TimeSpan.FromMilliseconds(100);
                default: return TimeSpan.FromMilliseconds(500);
            }
        }
    }
}