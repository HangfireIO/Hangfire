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
using System.Collections.Generic;
using System.Reflection;
using HangFire.Common;
using HangFire.Common.States;
using HangFire.Storage;

namespace HangFire.Client
{
    /// <summary>
    /// Provides low-level Client API. Creates jobs based on a 
    /// given <see cref="MethodData"/> data in a given <see cref="State"/>
    /// and puts them into the storage in an atomic way.
    /// </summary>
    internal class JobClient : IJobClient
    {
        private readonly IStorageConnection _connection;
        private readonly JobCreationPipeline _creationPipeline;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="JobClient"/> class
        /// with a specified Redis client manager and the default global
        /// <see cref="JobCreationPipeline"/> instance.
        /// </summary>
        public JobClient(IStorageConnection connection)
            : this(connection, JobCreationPipeline.Instance)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JobClient"/> class
        /// with a specified Redis client manager and a job creator.
        /// </summary>
        public JobClient(IStorageConnection connection, JobCreationPipeline creationPipeline)
        {
            if (connection == null) throw new ArgumentNullException("connection");
            if (creationPipeline == null) throw new ArgumentNullException("creationPipeline");

            _connection = connection;
            _creationPipeline = creationPipeline;
        }

        /// <summary>
        /// Creates a given job in a specified state in the storage.
        /// </summary>
        /// 
        /// <param name="job">Background job that will be created in a storage.</param>
        /// <param name="state">The initial state of the job.</param>
        /// <returns>The unique identifier of the created job.</returns>
        public string CreateJob(Job job, State state)
        {
            if (job == null) throw new ArgumentNullException("job");
            if (state == null) throw new ArgumentNullException("state");

            var parameters = job.MethodData.MethodInfo.GetParameters();

            ValidateMethodParameters(parameters);

            var context = new CreateContext(_connection, job, state);
            _creationPipeline.Run(context);

            return context.JobId;
        }

        /// <summary>
        /// Releases all resources used by the current instance
        /// of the <see cref="JobClient"/> class.
        /// </summary>
        public virtual void Dispose()
        {
            _connection.Dispose();
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
