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
using System.Reflection;
using HangFire.States;
using ServiceStack.Redis;

namespace HangFire.Client
{
    /// <summary>
    /// Provides a set of methods to create a job in the storage
    /// and apply its initial state.
    /// </summary>
    internal class JobClient : IJobClient
    {
        private readonly JobCreator _jobCreator;
        private readonly IRedisClient _redis;

        /// <summary>
        /// Initializes a new instance of the <see cref="JobClient"/> class
        /// with a specified Redis client manager and the default global
        /// <see cref="JobCreator"/> instance.
        /// </summary>
        public JobClient(IRedisClientsManager redisManager)
            : this(redisManager, JobCreator.Instance)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JobClient"/> class
        /// with a specified Redis client manager and a job creator.
        /// </summary>
        public JobClient(IRedisClientsManager redisManager, JobCreator jobCreator)
        {
            if (redisManager == null) throw new ArgumentNullException("redisManager");
            if (jobCreator == null) throw new ArgumentNullException("jobCreator");

            _redis = redisManager.GetClient();
            _jobCreator = jobCreator;
        }

        public string CreateJob(JobInvocationData data, string[] arguments, JobState state)
        {
            var parameters = data.Method.GetParameters();

            ValidateMethodParameters(parameters);

            var id = Guid.NewGuid().ToString();
            var descriptor = new ClientJobDescriptor(_redis, id, data, arguments, state);
            var context = new CreateContext(_redis, descriptor);

            _jobCreator.CreateJob(context);

            return id;
        }

        /// <summary>
        /// Releases all resources used by the current instance
        /// of the <see cref="JobClient"/> class.
        /// </summary>
        public virtual void Dispose()
        {
            _redis.Dispose();
        }

        private static void ValidateMethodParameters(IEnumerable<ParameterInfo> parameters)
        {
            foreach (var parameter in parameters)
            {
                // There is no guarantee that specified method will be invoked
                // in the same process. Therefore, output parameters and parameters
                // passed by reference are not supported.

                if (parameter.IsOut)
                {
                    throw new NotSupportedException(
                        "Output parameters are not supported: there is no guarantee that specified method will be invoked inside the same process.");
                }

                if (parameter.ParameterType.IsByRef)
                {
                    throw new ArgumentException(
                        "Parameters, passed by reference, are not supported: there is no guarantee that specified method will be invoked inside the same process.");
                }
            }
        }
    }
}
