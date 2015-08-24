// This file is part of Hangfire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// Hangfire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// Hangfire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with Hangfire. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Linq;
using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.States;

namespace Hangfire.Client
{
    public class CoreJobCreationProcess : IJobCreationProcess
    {
        private readonly IStateChangeProcess _stateMachine;

        public CoreJobCreationProcess()
            : this(new DefaultStateChangeProcess())
        {
        }

        public CoreJobCreationProcess([NotNull] IStateChangeProcess stateMachine)
        {
            if (stateMachine == null) throw new ArgumentNullException("stateMachine");
            _stateMachine = stateMachine;
        }

        public string Run(CreateContext context)
        {
            var parameters = context.Parameters.ToDictionary(x => x.Key, x => JobHelper.ToJson(x.Value));

            var createdAt = DateTime.UtcNow;
            var jobId = context.Connection.CreateExpiredJob(
                context.Job,
                parameters,
                createdAt,
                TimeSpan.FromHours(1));

            var backgroundJob = new BackgroundJob(jobId, context.Job, createdAt);

            if (context.InitialState != null)
            {
                var electContext = new ElectStateContext(
                    context.Storage, context.Connection, backgroundJob, context.InitialState, null);

                _stateMachine.ElectState(electContext);

                using (var transaction = context.Connection.CreateWriteTransaction())
                {
                    var applyContext = new ApplyStateContext(transaction, electContext);
                    _stateMachine.ApplyState(applyContext);

                    transaction.Commit();
                }
            }

            return backgroundJob.Id;
        }
    }
}