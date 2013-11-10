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
using System.Linq;
using HangFire.Filters;

namespace HangFire
{
    /// <summary>
    /// Represents the base class for job descriptors.
    /// </summary>
    public class JobDescriptor
    {
        internal JobDescriptor(string jobId, Type jobType)
        {
            JobId = jobId;
            Type = jobType;
        }

        internal JobDescriptor(string jobId, string jobType)
        {
            JobId = jobId;

            try
            {
                Type = Type.GetType(jobType, throwOnError: true);
            }
            catch (Exception ex)
            {
                TypeLoadException = ex;
            }
        }

        /// <summary>
        /// Gets the state of the creating job.
        /// </summary>
        public string JobId { get; private set; }

        /// <summary>
        /// Gets the type of the creating job.
        /// </summary>
        public Type Type { get; private set; }

        public Exception TypeLoadException { get; private set; }

        internal IEnumerable<JobFilterAttribute> GetFilterAttributes(bool useCache)
        {
            if (Type == null)
            {
                return Enumerable.Empty<JobFilterAttribute>();
            }

            if (useCache)
            {
                return ReflectedAttributeCache.GetTypeFilterAttributes(Type);
            }

            return Type
                .GetCustomAttributes(typeof(JobFilterAttribute), inherit: true)
                .Cast<JobFilterAttribute>();
        }
    }
}
