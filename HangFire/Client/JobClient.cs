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
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using HangFire.Common;
using HangFire.Common.States;
using HangFire.Storage;

namespace HangFire.Client
{
    /// <summary>
    /// Provides low-level Client API. Creates jobs based on a 
    /// given <see cref="JobMethod"/> data in a given <see cref="JobState"/>
    /// and puts them into the storage in an atomic way.
    /// </summary>
    internal class JobClient : IJobClient
    {
        private readonly IStorageConnection _connection;
        private readonly JobCreator _jobCreator;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="JobClient"/> class
        /// with a specified Redis client manager and the default global
        /// <see cref="JobCreator"/> instance.
        /// </summary>
        public JobClient(IStorageConnection connection)
            : this(connection, JobCreator.Instance)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JobClient"/> class
        /// with a specified Redis client manager and a job creator.
        /// </summary>
        public JobClient(IStorageConnection connection, JobCreator jobCreator)
        {
            if (connection == null) throw new ArgumentNullException("connection");
            if (jobCreator == null) throw new ArgumentNullException("jobCreator");

            _connection = connection;
            _jobCreator = jobCreator;
        }

        /// <summary>
        /// Creates a job with a given <paramref name="method"/>, <paramref name="arguments"/>
        /// and a <paramref name="state"/>, runs registered client filters and 
        /// puts it into the storage.
        /// </summary>
        /// 
        /// <remarks>
        /// Each argument should be serialized into a string using the 
        /// <see cref="TypeConverter.ConvertToInvariantString(object)"/> method of
        /// a corresponding <see cref="TypeConverter"/> instance.
        /// </remarks>
        /// 
        /// <param name="method">Method that will be called during the performance of the job.</param>
        /// <param name="arguments">Serialized arguments that will be passed to a <paramref name="method"/>.</param>
        /// <param name="state">The initial state of the job.</param>
        /// <returns>The unique identifier of the created job.</returns>
        public string CreateJob(JobMethod method, string[] arguments, JobState state)
        {
            if (method == null) throw new ArgumentNullException("method");
            if (arguments == null) throw new ArgumentNullException("arguments");
            if (state == null) throw new ArgumentNullException("state");

            var parameters = method.Method.GetParameters();

            ValidateMethodParameters(parameters);

            var context = new CreateContext(_connection, method, arguments, state);
            _jobCreator.CreateJob(context);

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
