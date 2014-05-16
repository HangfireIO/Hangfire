using System;
using Common.Logging;
using HangFire.Common;
using HangFire.States;

namespace HangFire
{
    public class AutomaticRetryAttribute : JobFilterAttribute, IElectStateFilter
    {
        private const int DefaultRetryAttempts = 10;

        private int _attempts;
        
        public AutomaticRetryAttribute()
        {
            Attempts = DefaultRetryAttempts;
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

        public void OnStateElection(ElectStateContext context)
        {
            var failedState = context.CandidateState as FailedState;
            if (failedState == null)
            {
                // This filter accepts only failed job state.
                return;
            }

            var retryAttempt = context.GetJobParameter<int>("RetryCount") + 1;
            
            if (retryAttempt < Attempts)
            {
                var delay = TimeSpan.FromSeconds(SecondsToDelay(retryAttempt));

                context.SetJobParameter("RetryCount", retryAttempt);

                // If attempt number is less than max attempts, we should
                // schedule the job to run again later.
                context.CandidateState = new ScheduledState(delay)
                {
                    Reason = String.Format("Retry attempt {0} of {1}", retryAttempt, Attempts)
                };
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