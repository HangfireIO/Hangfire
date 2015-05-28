using System;
using Hangfire.Common;
using Hangfire.Logging;
using Hangfire.States;
using Hangfire.Storage;

namespace Hangfire
{
    public enum AttemptsExceededAction
    {
        Fail = 0,
        Delete
    }

    public sealed class AutomaticRetryAttribute : JobFilterAttribute, IElectStateFilter, IApplyStateFilter
    {
        private static readonly ILog Logger = LogProvider.GetLogger(typeof (AutomaticRetryAttribute));
        private const int DefaultRetryAttempts = 10;

        private int _attempts;

        public AutomaticRetryAttribute()
        {
            Attempts = DefaultRetryAttempts;
            LogEvents = true;
            OnAttemptsExceeded = AttemptsExceededAction.Fail;
        }

        public int Attempts
        {
            get { return _attempts; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value", "Attempts value must be equal or greater than zero.");
                }
                _attempts = value;
            }
        }

        public AttemptsExceededAction OnAttemptsExceeded { get; set; }

        public bool LogEvents { get; set; }

        public void OnStateElection(ElectStateContext context)
        {
            var failedState = context.CandidateState as FailedState;
            if (failedState == null)
            {
                // This filter accepts only failed job state.
                return;
            }

            var retryAttempt = context.GetJobParameter<int>("RetryCount") + 1;

            if (retryAttempt <= Attempts)
            {
                ScheduleAgainLater(context, retryAttempt, failedState);
            }
            else if (retryAttempt > Attempts && OnAttemptsExceeded == AttemptsExceededAction.Delete)
            {
                TransitionToDeleted(context, failedState);
            }
            else
            {
                if (LogEvents)
                {
                    Logger.ErrorException(
                        String.Format(
                            "Failed to process the job '{0}': an exception occurred.",
                            context.JobId),
                        failedState.Exception);
                }
            }
        }

        /// <summary>
        /// Schedules the job to run again later. See <see cref="SecondsToDelay"/>.
        /// </summary>
        /// <param name="context">The state context.</param>
        /// <param name="retryAttempt">The count of retry attempts made so far.</param>
        /// <param name="failedState">Object which contains details about the current failed state.</param>
        private void ScheduleAgainLater(ElectStateContext context, int retryAttempt, FailedState failedState)
        {
            context.SetJobParameter("RetryCount", retryAttempt);

            var delay = TimeSpan.FromSeconds(SecondsToDelay(retryAttempt));

            const int maxMessageLength = 50;
            var exceptionMessage = failedState.Exception.Message;

            // If attempt number is less than max attempts, we should
            // schedule the job to run again later.
            context.CandidateState = new ScheduledState(delay)
            {
                Reason = String.Format(
                    "Retry attempt {0} of {1}: {2}", 
                    retryAttempt, 
                    Attempts,
                    exceptionMessage.Length > maxMessageLength
                    ? exceptionMessage.Substring(0, maxMessageLength - 1) + "…"
                    : exceptionMessage)
            };

            if (LogEvents)
            {
                Logger.WarnException(
                    String.Format(
                        "Failed to process the job '{0}': an exception occurred. Retry attempt {1} of {2} will be performed in {3}.",
                        context.JobId,
                        retryAttempt,
                        Attempts,
                        delay),
                    failedState.Exception);
            }
        }

        /// <summary>
        /// Transition the candidate state to the deleted state.
        /// </summary>
        /// <param name="context">The state context.</param>
        /// <param name="failedState">Object which contains details about the current failed state.</param>
        private void TransitionToDeleted(ElectStateContext context, FailedState failedState)
        {
            context.CandidateState = new DeletedState
            {
                Reason = String.Format("Automatic deletion after retry count exceeded {0}", Attempts)
            };

            if (LogEvents)
            {
                Logger.WarnException(
                    String.Format(
                        "Failed to process the job '{0}': an exception occured. Job was automatically deleted because the retry attempt count exceeded {1}.",
                        context.JobId,
                        Attempts),
                    failedState.Exception);
            }
        }

        // delayed_job uses the same basic formula
        private static int SecondsToDelay(long retryCount)
        {
            var random = new Random();
            return (int)Math.Round(
                Math.Pow(retryCount - 1, 4) + 15 + (random.Next(30) * (retryCount)));
        }

        public void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
            if (context.NewState is ScheduledState && 
                context.NewState.Reason != null && 
                context.NewState.Reason.StartsWith("Retry attempt"))
            {
                transaction.AddToSet("retries", context.JobId);
            }
        }

        public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
            if (context.OldStateName == ScheduledState.StateName)
            {
                transaction.RemoveFromSet("retries", context.JobId);
            }
        }
    }
}