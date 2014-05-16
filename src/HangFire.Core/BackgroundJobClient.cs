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
using HangFire.Client;
using HangFire.Common;
using HangFire.States;
using HangFire.Storage;

namespace HangFire
{
    /// <summary>
    /// Represents a HangFire Client API. Contains methods related
    /// to the job creation feature.
    /// </summary>
    public class BackgroundJobClient : IBackgroundJobClient
    {
        private readonly IJobCreationProcess _process;

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundJobClient"/> class
        /// with the default connection and default global 
        /// <see cref="JobCreationProcess"/> instance.
        /// </summary>
        public BackgroundJobClient()
            : this(JobStorage.Current)
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundJobClient"/> class
        /// with a specified storage and the default global
        /// <see cref="JobCreationProcess"/> instance.
        /// </summary>
        public BackgroundJobClient(JobStorage storage)
            : this(storage, new StateMachineFactory(storage))
        {
        }

        public BackgroundJobClient(JobStorage storage, IStateMachineFactory stateMachineFactory)
            : this(storage, stateMachineFactory, JobCreationProcess.Instance)
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

            Storage = storage;
            Connection = storage.GetConnection();
            StateMachineFactory = stateMachineFactory;

            _process = process;
        }

        /// <inheritdoc />
        public JobStorage Storage { get; private set; }

        /// <inheritdoc />
        public IStorageConnection Connection { get; private set; }

        /// <inheritdoc />
        public IStateMachineFactory StateMachineFactory { get; private set; }

        /// <inheritdoc />
        public string Create(Job job, IState state)
        {
            if (job == null) throw new ArgumentNullException("job");
            if (state == null) throw new ArgumentNullException("state");

            try
            {
                var context = new CreateContext(Connection, StateMachineFactory, job, state);
                _process.Run(context);

                return context.JobId;
            }
            catch (Exception ex)
            {
                throw new CreateJobFailedException("Job creation process has bee failed. See inner exception for details", ex);
            }
        }

        /// <summary>
        /// Releases all resources used by the current instance
        /// of the <see cref="BackgroundJobClient"/> class.
        /// </summary>
        public virtual void Dispose()
        {
            Connection.Dispose();
        }
    }
}
