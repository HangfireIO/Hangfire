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
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.States;

namespace Hangfire
{
    /// <summary>
    /// Represents a Hangfire Client API. Contains methods related
    /// to the job creation feature.
    /// </summary>
    public class BackgroundJobClient : IBackgroundJobClient
    {
        private readonly JobStorage _storage;
        private readonly IJobCreationProcess _process;
        private readonly IStateMachineFactory _stateMachineFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundJobClient"/> class
        /// with the default connection and default global 
        /// <see cref="DefaultJobCreationProcess"/> instance.
        /// </summary>
        public BackgroundJobClient()
            : this(JobStorage.Current)
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundJobClient"/> class
        /// with a specified storage and the default global
        /// <see cref="DefaultJobCreationProcess"/> instance.
        /// </summary>
        public BackgroundJobClient(JobStorage storage)
            : this(storage, new StateMachineFactory(storage))
        {
        }

        public BackgroundJobClient(JobStorage storage, IStateMachineFactory stateMachineFactory)
            : this(storage, stateMachineFactory, DefaultJobCreationProcess.Instance)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundJobClient"/> class
        /// with a specified job storage and job creation process.
        /// </summary>
        /// 
        /// <exception cref="ArgumentNullException"><paramref name="storage"/> argument is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="process"/> argument is null.</exception>
        public BackgroundJobClient(
            JobStorage storage, 
            IStateMachineFactory stateMachineFactory, 
            IJobCreationProcess process)
        {
            if (storage == null) throw new ArgumentNullException("storage");
            if (stateMachineFactory == null) throw new ArgumentNullException("stateMachineFactory");
            if (process == null) throw new ArgumentNullException("process");
            
            _storage = storage;
            _stateMachineFactory = stateMachineFactory;
            _process = process;
        }

        /// <inheritdoc />
        public string Create(Job job, IState state)
        {
            if (job == null) throw new ArgumentNullException("job");
            if (state == null) throw new ArgumentNullException("state");

            try
            {
                using (var connection = _storage.GetConnection())
                {
                    var context = new CreateContext(connection, _stateMachineFactory, job, state);
                    return _process.Run(context);
                }
            }
            catch (Exception ex)
            {
                throw new CreateJobFailedException("Job creation process has bee failed. See inner exception for details", ex);
            }
        }

        /// <inheritdoc />
        public bool ChangeState(string jobId, IState state, string fromState)
        {
            if (jobId == null) throw new ArgumentNullException("jobId");
            if (state == null) throw new ArgumentNullException("state");

            using (var connection = _storage.GetConnection())
            {
                var stateMachine = _stateMachineFactory.Create(connection);
                return stateMachine.ChangeState(jobId, state, fromState != null ? new[] { fromState } : null);
            }
        }
    }
}
