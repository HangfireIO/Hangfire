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

using HangFire.States;
using ServiceStack.Redis;

namespace HangFire.Client
{
    public interface IJobClient : IDisposable
    {
        void CreateJob(string id, JobInvocationData invocationData, JobState state);
    }

    public class JobInvocationData
    {
        public Type Type { get; set; }
        public MethodInfo Method { get; set; }
        public List<Tuple<Type, object>> Parameters { get; set; }
    }

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

        public void CreateJob(string id, JobInvocationData invocationData, JobState state)
        {
            try
            {
                var arguments = new List<ArgumentPair>(invocationData.Parameters.Count);
                foreach (var parameter in invocationData.Parameters)
                {
                    var converter = TypeDescriptor.GetConverter(parameter.Item1);
                    var value = converter.ConvertToInvariantString(parameter.Item2);

                    arguments.Add(new ArgumentPair { Type = parameter.Item1, Value = value });
                }

                var descriptor = new ClientJobDescriptor(_redis, id, invocationData.Type, invocationData.Method, state);
                descriptor.SetParameter("Args", arguments);

                var context = new CreateContext(_redis, descriptor);

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
    }
}
