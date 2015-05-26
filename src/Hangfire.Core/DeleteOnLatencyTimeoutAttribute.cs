using System;
using Hangfire.Common;
using Hangfire.Logging;
using Hangfire.States;
using Hangfire.Storage;

namespace Hangfire
{
    public class DeleteOnLatencyTimeoutAttribute : JobFilterAttribute, IElectStateFilter
    {
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();
        private int _timeoutInSeconds;
        private const int DefaultTimeoutInSeconds = 300;


        public bool LogEvents { get; set; }
        public int TimeoutInSeconds
        {
            get { return _timeoutInSeconds; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value", "TimeoutInSeconds value must be equal or greater that zero.");
                }
                _timeoutInSeconds = value;
            }
        }

        public DeleteOnLatencyTimeoutAttribute()
        {
            TimeoutInSeconds = DefaultTimeoutInSeconds;
            LogEvents = true;
        }

        public void OnStateElection(ElectStateContext context)
        {
            var state = context.CandidateState as ProcessingState;
            if (state == null)
            {
                //this filter only accepts Processing
                return;
            }

            var elapsedTime = DateTime.UtcNow - context.CreatedAt;

            if (elapsedTime.TotalSeconds > TimeoutInSeconds)
            {
                context.CandidateState = new DeletedState()
                {
                    Reason = String.Format("Job has exceeded latency timeout of {0} seconds", TimeoutInSeconds)
                };

                if (LogEvents)
                {
                    Logger.WarnFormat(
                        "Failed to process the job '{0}': Job has exceeded latency timeout of {1} seconds",
                        context.JobId,
                        TimeoutInSeconds
                        );
                }
            }
        }

    }
}