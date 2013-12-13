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
using System.ComponentModel;
using System.Reflection;
using HangFire.Common;
using HangFire.Common.States;
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

        public string CreateJob(JobMethod method, string[] arguments, JobState state)
        {
            var parameters = method.Method.GetParameters();

            ValidateMethodParameters(parameters);

            var id = Guid.NewGuid().ToString();

            var job = method.Serialize();
            job["Arguments"] = JobHelper.ToJson(arguments);
            job["CreatedAt"] = JobHelper.ToStringTimestamp(DateTime.UtcNow);

            var context = new CreateContext(_redis, id, method, job, state);
            _jobCreator.CreateJob(context);

            return id;
        }

        /// <summary>
        /// Creates a job of the specified <paramref name="type"/> with the
        /// given unique identifier and arguments provided in a anonymous object
        /// in the <paramref name="args"/> parameter. After creating the job, the
        /// specified <paramref name="state"/> will be applied to it.
        /// </summary>
        /// 
        /// <remarks>
        /// Job Arguments
        /// 
        /// Specified anonymous object in the <paramref name="args"/> parameter will
        /// be converted into an instance of the <see cref="Dictionary{String, String}"/>
        /// class.
        /// Each anynymous property will act as a key, and each property value will act
        /// as a key value. However, the values will be converted from its actual
        /// type to a <see cref="string"/> type using the corresponding instance of the
        /// <see cref="TypeConverter"/> class. If the actual type has custom 
        /// converter defined and it throws an exception, then the 
        /// <see cref="InvalidOperationException"/> will be re-thrown.
        /// </remarks>
        /// 
        /// <param name="id">The unique identifier of the new job.</param>
        /// <param name="type">The type of the job.</param>
        /// <param name="state">The state, which will be applied when the job created.</param>
        /// <param name="args">Arguments of the job as an anonymous object.</param>
        public void CreateJob(
            string id, Type type, JobState state, object args)
        {
            CreateJob(id, type, state, PropertiesToDictionary(args));
        }

        /// <summary>
        /// Creates a job of the specified <paramref name="type"/> with the
        /// given unique identifier and arguments provided in the <paramref name="args"/>
        /// parameter. After creating the job, the specified <paramref name="state"/>
        /// will be applied to it.
        /// </summary>
        /// 
        /// <remarks>
        /// Job Arguments
        /// 
        /// The <paramref name="args"/> parameter contains a dictionary of job
        /// arguments. Each argument has a name (the key) and a value (the value).
        /// When the job is being processed on a Server, these arguments are deserialized
        /// into strongly-typed properties of the job instance.
        /// 
        /// The deserialization method uses <see cref="TypeConverter"/> class to 
        /// change the argument type from string to its actual type (the type of the
        /// corresponding property). So, you need to serialize the values using
        /// the actual type's <see cref="TypeConverter"/> (if its actual type
        /// differs from the <see cref="string"/> type). Otherwise the job could
        /// be failed.
        /// </remarks>
        /// 
        /// <param name="id">The unique identifier of the new job.</param>
        /// <param name="type">The type of the job.</param>
        /// <param name="state">The state, which will be applied when the job created.</param>
        /// <param name="args">Arguments of the job.</param>
        public void CreateJob(
            string id, Type type, JobState state, IDictionary<string, string> args)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException("id");
            if (type == null) throw new ArgumentNullException("type");
            if (state == null) throw new ArgumentNullException("state");
            if (args == null) throw new ArgumentNullException("args");

            if (!typeof(BackgroundJob).IsAssignableFrom(type))
            {
                throw new ArgumentException(
                    String.Format("The type '{0}' must inherit the '{1}' type.", type, typeof(BackgroundJob)),
                    "type");
            }

            try
            {
                var method = new JobMethod(type);

                var job = method.Serialize();
                job["Args"] = JobHelper.ToJson(args);
                job["CreatedAt"] = JobHelper.ToStringTimestamp(DateTime.UtcNow);

                var context = new CreateContext(_redis, id, method, job, state);
                _jobCreator.CreateJob(context);
            }
            catch (Exception ex)
            {
                throw new CreateJobFailedException(
                    "Job creation was failed. See the inner exception for details.", 
                    ex);
            }
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

         private static IDictionary<string, string> PropertiesToDictionary(object obj)
        {
            var result = new Dictionary<string, string>();
            if (obj == null) return result;

            foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(obj))
            {
                var propertyValue = descriptor.GetValue(obj);
                string value = null;

                if (propertyValue != null)
                {
                    var converter = TypeDescriptor.GetConverter(propertyValue.GetType());

                    try
                    {
                        value = converter.ConvertToInvariantString(propertyValue);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException(
                            String.Format(
                                "Could not convert property '{0}' of type '{1}' to a string using the '{2}'. See the inner exception for details.",
                                descriptor.Name,
                                descriptor.PropertyType,
                                converter.GetType()),
                            ex);
                    }
                }

                result.Add(descriptor.Name, value);
            }

            return result;
        }
    }
}
