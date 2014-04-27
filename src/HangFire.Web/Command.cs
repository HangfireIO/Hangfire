// This file is part of HangFire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// HangFire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// HangFire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with HangFire. If not, see <http://www.gnu.org/licenses/>.

using System;
using HangFire.States;

namespace HangFire.Web
{
    internal static class Command
    {
        public static readonly Func<string, bool> Retry 
            = jobId =>
            {
                using (var connection = JobStorage.Current.GetConnection())
                {
                    // TODO: clear retry attempts counter.

                    var factory = new StateMachineFactory(JobStorage.Current);
                    var stateMachine = factory.Create(connection);
                    var state = new EnqueuedState
                    {
                        Reason = "The job has been retried by a user"
                    };

                    return stateMachine.TryToChangeState(jobId, state, new [] { FailedState.StateName });
                }
            };

        public static readonly Func<string, bool> EnqueueScheduled 
            = jobId =>
            {
                using (var connection = JobStorage.Current.GetConnection())
                {
                    var factory = new StateMachineFactory(JobStorage.Current);
                    var stateMachine = factory.Create(connection);
                    var state = new EnqueuedState{
                        Reason = "Scheduled job has been enqueued by a user"
                    };

                    return stateMachine.TryToChangeState(jobId, state, new [] { ScheduledState.StateName });
                }
            };
    }
}
