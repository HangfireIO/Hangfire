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
using HangFire.Client;

namespace HangFire
{
    /// <summary>
    /// Represents the base class for job descriptors.
    /// </summary>
    public abstract class JobDescriptor
    {
        internal JobDescriptor(string jobId, JobMethod method)
        {
            if (jobId == null) throw new ArgumentNullException("jobId");

            JobId = jobId;
            Method = method;
        }

        /// <summary>
        /// Gets the state of the creating job.
        /// </summary>
        public string JobId { get; private set; }

        public JobMethod Method { get; private set; }

        public virtual void SetParameter(string name, object value)
        {
            throw new NotSupportedException("Setting parameters is not allowed in this context.");
        }

        public virtual T GetParameter<T>(string name)
        {
            throw new NotSupportedException("Getting parameters is not allowed in this context.");
        }
    }
}
