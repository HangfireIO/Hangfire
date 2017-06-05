namespace HangFire.SimpleInjector
{
    #region

    using System;

    using global::Common.Logging;

    using HangFire.Common;
    using HangFire.States;

    #endregion

    public class AutomaticRetryWithDeletionAttribute : JobFilterAttribute, IElectStateFilter
    {
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();

        private const int DefaultRetryAttempts = 10;

        private int attempts;

        public AutomaticRetryWithDeletionAttribute()
        {
            Attempts = DefaultRetryAttempts;
            LogEvents = true;
        }

        public int Attempts
        {
            get
            {
                return attempts;
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value", "Attempts value must be equal or greater that zero.");
                }
                attempts = value;
            }
        }

        public bool LogEvents { get; set; }

        public void OnStateElection(ElectStateContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            var failedState = context.CandidateState as FailedState;
            if (failedState == null)
            {
                // This filter accepts only failed job state.
                return;
            }

            var retryAttempt = context.GetJobParameter<int>("RetryCount") + 1;

            if (retryAttempt <= Attempts)
            {
                var delay = TimeSpan.FromSeconds(SecondsToDelay(retryAttempt));

                context.SetJobParameter("RetryCount", retryAttempt);

                // If attempt number is less than max attempts, we should
                // schedule the job to run again later.
                context.CandidateState = new ScheduledState(delay)
                                         {
                                             Reason =
                                                 String.Format(
                                                     "Retry attempt {0} of {1}",
                                                     retryAttempt,
                                                     Attempts)
                                         };

                if (LogEvents)
                {
                    Logger.WarnFormat(
                        "Failed to process the job '{0}': an exception occurred. Retry attempt {1} of {2} will be performed in {3}.",
                        failedState.Exception,
                        context.JobId,
                        retryAttempt,
                        Attempts,
                        delay);
                }
            }
            //delete the job if retry attempts are exceeded
            if (retryAttempt > Attempts)
            {
                context.CandidateState = new DeletedState
                                         {
                                             Reason =
                                                 String.Format(
                                                     "Job is deleted because the Retry attempt {0} exceeded {1}",
                                                     retryAttempt,
                                                     Attempts)
                                         };
                if (LogEvents)
                {
                    Logger.WarnFormat(
                        "Failed to process the job '{0}': an exception occurred. because retry attempts are exceeded.",
                        failedState.Exception,
                        context.JobId,
                        retryAttempt,
                        Attempts);
                }
            }
            else
            {
                if (LogEvents)
                {
                    Logger.ErrorFormat(
                        "Failed to process the job '{0}': an exception occurred.",
                        failedState.Exception,
                        context.JobId);
                }
            }
        }

        // delayed_job uses the same basic formula
        private static int SecondsToDelay(long retryCount)
        {
            var random = new Random();
            return (int)Math.Round(Math.Pow(retryCount - 1, 4) + 15 + (random.Next(30) * (retryCount)));
        }
    }
}
