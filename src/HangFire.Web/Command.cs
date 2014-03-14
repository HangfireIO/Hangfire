// This file is part of HangFire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// HangFire is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// HangFire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with HangFire.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Threading;
using HangFire.States;
using HangFire.Storage;

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

                    var stateMachine = new StateMachine(connection);
                    var state = new EnqueuedState
                    {
                        Reason = "The job has been retried by a user"
                    };

                    return stateMachine.ChangeState(jobId, state, FailedState.Name);
                }
            };

        public static readonly Func<string, bool> EnqueueScheduled 
            = jobId =>
            {
                using (var connection = JobStorage.Current.GetConnection())
                {
                    var stateMachine = new StateMachine(connection);
                    var state = new EnqueuedState{
                        Reason = "Scheduled job has been enqueued by a user"
                    };

                    return stateMachine.ChangeState(jobId, state, ScheduledState.Name);
                }
            };
    }
}
