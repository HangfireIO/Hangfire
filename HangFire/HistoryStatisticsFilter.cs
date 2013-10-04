using System;
using HangFire.States;
using ServiceStack.Redis;

namespace HangFire
{
    public class HistoryStatisticsFilter : IJobStateFilter
    {
        public JobState OnStateChanged(IRedisClient redis, JobState state)
        {
            return state;
        }

        public void OnStateApplied(IRedisTransaction transaction, JobState state)
        {
            if (state.StateName == SucceededState.Name)
            {
                transaction.QueueCommand(x => x.IncrementValue(
                String.Format("hangfire:stats:succeeded:{0}", DateTime.UtcNow.ToString("yyyy-MM-dd"))));

                transaction.QueueCommand(x => x.EnqueueItemOnList("hangfire:succeeded", state.JobId));
                transaction.QueueCommand(x => x.TrimList("hangfire:succeeded", 0, 99));

                var hourlySucceededKey = String.Format(
                    "hangfire:stats:succeeded:{0}",
                    DateTime.UtcNow.ToString("yyyy-MM-dd-HH"));
                transaction.QueueCommand(x => x.IncrementValue(hourlySucceededKey));
                transaction.QueueCommand(x => x.ExpireEntryIn(hourlySucceededKey, TimeSpan.FromDays(1)));
            }
            else if (state.StateName == FailedState.Name)
            {
                transaction.QueueCommand(x => x.IncrementValue(
                String.Format("hangfire:stats:failed:{0}", DateTime.UtcNow.ToString("yyyy-MM-dd"))));

                transaction.QueueCommand(x => x.IncrementValue(
                    String.Format("hangfire:stats:failed:{0}", DateTime.UtcNow.ToString("yyyy-MM-ddTHH-mm"))));

                var hourlyFailedKey = String.Format(
                    "hangfire:stats:failed:{0}",
                    DateTime.UtcNow.ToString("yyyy-MM-dd-HH"));
                transaction.QueueCommand(x => x.IncrementValue(hourlyFailedKey));
                transaction.QueueCommand(x => x.ExpireEntryIn(hourlyFailedKey, TimeSpan.FromDays(1)));
            }
        }

        public void OnStateUnapplied(IRedisTransaction transaction, string state)
        {
        }
    }
}
