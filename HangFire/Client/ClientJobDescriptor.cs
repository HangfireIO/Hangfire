// This file is part of HangFire.
// Copyright © 2013 Sergey Odinokov.
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
using System.Collections.Generic;
using HangFire.Filters;
using HangFire.States;
using ServiceStack.Redis;

namespace HangFire.Client
{
    /// <summary>
    /// Provides information about the job being created.
    /// </summary>
    public class ClientJobDescriptor 
    {
        private readonly StateMachine _stateMachine;

        private readonly IDictionary<string, string> _jobParameters;

        private bool _jobWasCreated;

        internal ClientJobDescriptor(
            IRedisClient redis,
            string jobId, 
            JobMethod jobMethod,
            string[] arguments,
            JobState state)
        {
            if (redis == null) throw new ArgumentNullException("redis");
            if (jobId == null) throw new ArgumentNullException("jobId");
            if (jobMethod == null) throw new ArgumentNullException("jobMethod");
            if (state == null) throw new ArgumentNullException("state");

            _stateMachine = new StateMachine(redis);

            _jobParameters = jobMethod.Serialize();
            _jobParameters["Arguments"] = JobHelper.ToJson(arguments);
            _jobParameters["CreatedAt"] = JobHelper.ToStringTimestamp(DateTime.UtcNow);

            JobId = jobId;
            JobMethod = jobMethod;
            State = state;
        }

        public string JobId { get; private set; }
        public JobMethod JobMethod { get; private set; }

        /// <summary>
        /// Gets the initial state of the creating job. Note, that
        /// the final state of the created job could be changed after 
        /// the registered instances of the <see cref="IStateChangingFilter"/>
        /// class are doing their job.
        /// </summary>
        public JobState State { get; private set; }

        /// <summary>
        /// Sets the job parameter of the specified <paramref name="name"/>
        /// to the corresponding <paramref name="value"/>. The value of the
        /// parameter is being serialized to a JSON string.
        /// </summary>
        /// 
        /// <param name="name">The name of the parameter.</param>
        /// <param name="value">The value of the parameter.</param>
        /// 
        /// <exception cref="ArgumentNullException">The <paramref name="name"/> is null or empty.</exception>
        public void SetParameter(string name, object value)
        {
            if (String.IsNullOrWhiteSpace(name)) throw new ArgumentNullException("name");

            if (_jobWasCreated)
            {
                throw new InvalidOperationException("Could not set parameter for a created job.");
            }

            _jobParameters.Add(name, JobHelper.ToJson(value));
        }

        /// <summary>
        /// Gets the job parameter of the specified <paramref name="name"/>
        /// if it exists. The parameter is being deserialized from a JSON 
        /// string value to the given type <typeparamref name="T"/>.
        /// </summary>
        /// 
        /// <typeparam name="T">The type of the parameter.</typeparam>
        /// <param name="name">The name of the parameter.</param>
        /// <returns>The value of the given parameter if it exists or null otherwise.</returns>
        /// 
        /// <exception cref="ArgumentNullException">The <paramref name="name"/> is null or empty.</exception>
        /// <exception cref="NotSupportedException">Could not deserialize the parameter value to the type <typeparamref name="T"/>.</exception>
        public T GetParameter<T>(string name)
        {
            if (String.IsNullOrWhiteSpace(name)) throw new ArgumentNullException("name");

            return _jobParameters.ContainsKey(name)
                ? JobHelper.FromJson<T>(_jobParameters[name])
                : default(T);
        }

        internal void Create()
        {
            _jobWasCreated = true;
            _stateMachine.CreateInState(JobId, JobMethod, _jobParameters, State);
        }
    }
}
