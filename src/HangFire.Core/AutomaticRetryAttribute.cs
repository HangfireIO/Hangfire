using System;
using Common.Logging;
using HangFire.Common;
using HangFire.States;

namespace HangFire
{
    public class AutomaticRetryAttribute : JobFilterAttribute, IElectStateFilter
    {
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();
        private const int DefaultRetryAttempts = 10;

        private int _attempts;
        
        public AutomaticRetryAttribute()
        {
            Attempts = DefaultRetryAttempts;
            LogEvents = true;
        }

        public int Attempts
        {
            get { return _attempts; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value", "Attempts value must be equal or greater that zero.");
                }
                _attempts = value;
            }
        }

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
                var delay = TimeSpan.FromSeconds(SecondsToDelay(retryAttempt));

                context.SetJobParameter("RetryCount", retryAttempt);

                // If attempt number is less than max attempts, we should
                // schedule the job to run again later.
                context.CandidateState = new ScheduledState(delay)
                {
                    Reason = String.Format("Retry attempt {0} of {1}", retryAttempt, Attempts)
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
            return (int)Math.Round(
                Math.Pow(retryCount - 1, 4) + 15 + (random.Next(30) * (retryCount)));
        }
    }
}