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
using Hangfire.Annotations;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.States;

namespace Hangfire
{
    /// <summary>
    /// Provides methods for creating all the types of background jobs and 
    /// changing their states. Represents a default implementation of the 
    /// <see cref="IBackgroundJobClient"/> interface.
    /// </summary>
    /// 
    /// <remarks><see cref="BackgroundJobClient"/> class uses the 
    /// <see cref="IBackgroundJobFactory"/> interface for creating background 
    /// jobs, and the <see cref="IBackgroundJobStateChanger"/> interface for 
    /// changing their states. Please see these types and their implementations 
    /// to learn the details.</remarks>
    /// 
    /// <threadsafety static="true" instance="true" />
    /// 
    /// <seealso cref="IBackgroundJobClient"/>
    public class BackgroundJobClient : IBackgroundJobClient
    {
        private readonly JobStorage _storage;
        private readonly IBackgroundJobFactory _factory;
        private readonly IBackgroundJobStateChanger _stateChanger;

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundJobClient"/> class
        /// with the default connection and default global 
        /// <see cref="BackgroundJobFactory"/> instance.
        /// </summary>
        public BackgroundJobClient()
            : this(JobStorage.Current)
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundJobClient"/>
        /// class with a specified storage and the default global
        /// <see cref="BackgroundJobFactory"/> instance.
        /// </summary>
        public BackgroundJobClient([NotNull] JobStorage storage)
            : this(storage, new BackgroundJobFactory(), new BackgroundJobStateChanger())
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundJobClient"/> class
        /// with the specified job storage and job creation factory.
        /// </summary>
        /// 
        /// <exception cref="ArgumentNullException"><paramref name="storage"/> argument is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="factory"/> argument is null.</exception>
        public BackgroundJobClient(
            [NotNull] JobStorage storage,
            [NotNull] IBackgroundJobFactory factory,
            [NotNull] IBackgroundJobStateChanger stateChanger)
        {
            if (storage == null) throw new ArgumentNullException("storage");
            if (factory == null) throw new ArgumentNullException("factory");
            if (stateChanger == null) throw new ArgumentNullException("stateChanger");
            
            _storage = storage;
            _stateChanger = stateChanger;
            _factory = factory;
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
                    var context = new CreateContext(_storage, connection, job, state);
                    var backroundJob = _factory.Create(context);

                    return backroundJob != null ? backroundJob.Id : null;
                }
            }
            catch (Exception ex)
            {
                throw new BackgroundJobClientException("Background job creation failed. See inner exception for details.", ex);
            }
        }

        /// <inheritdoc />
        public bool ChangeState(string jobId, IState state, string expectedState)
        {
            if (jobId == null) throw new ArgumentNullException("jobId");
            if (state == null) throw new ArgumentNullException("state");

            try
            {
                using (var connection = _storage.GetConnection())
                {
                    var appliedState = _stateChanger.ChangeState(new StateChangeContext(
                        _storage,
                        connection,
                        jobId,
                        state,
                        expectedState != null ? new[] { expectedState } : null));

                    return appliedState != null && appliedState.Name.Equals(state.Name, StringComparison.OrdinalIgnoreCase);
                }
            }
            catch (Exception ex)
            {
                throw new BackgroundJobClientException("State change of a background job failed. See inner exception for details", ex);
            }
        }
    }
}
