using System;
using System.Threading;
using Common.Logging;
using HangFire.Common;
using HangFire.States;

namespace HangFire.Server.Components
{
    public class SchedulePoller2 : IServerComponent
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(SchedulePoller2));

        private readonly JobStorage _storage;
        private readonly IStateMachineFactory _stateMachineFactory;
        private readonly TimeSpan _pollInterval;

        private int _enqueuedCount;

        public SchedulePoller2(
            JobStorage storage, 
            IStateMachineFactory stateMachineFactory, 
            TimeSpan pollInterval)
        {
            if (storage == null) throw new ArgumentNullException("storage");
            if (stateMachineFactory == null) throw new ArgumentNullException("stateMachineFactory");

            _storage = storage;
            _stateMachineFactory = stateMachineFactory;
            _pollInterval = pollInterval;
        }

        public void Execute(CancellationToken cancellationToken)
        {
            if (!EnqueueNextScheduledJob())
            {
                if (_enqueuedCount != 0)
                {
                    Logger.InfoFormat("{0} scheduled jobs were enqueued.", _enqueuedCount);
                    _enqueuedCount = 0;
                }

                cancellationToken.WaitHandle.WaitOne(_pollInterval);
            }
            else
            {
                // No wait, try to fetch next scheduled job immediately.
                _enqueuedCount++;
            }
        }

        private bool EnqueueNextScheduledJob()
        {
            using (var connection = _storage.GetConnection())
            {
                var timestamp = JobHelper.ToTimestamp(DateTime.UtcNow);

                // TODO: it is very slow. Add batching.
                var jobId = connection
                    .GetFirstByLowestScoreFromSet("schedule", 0, timestamp);

                if (String.IsNullOrEmpty(jobId))
                {
                    return false;
                }

                var stateMachine = _stateMachineFactory.Create(connection);
                var enqueuedState = new EnqueuedState
                {
                    Reason = "Enqueued as a scheduled job"
                };

                stateMachine.TryToChangeState(jobId, enqueuedState, new[] { ScheduledState.StateName });

                return true;
            }
        }
    }
}