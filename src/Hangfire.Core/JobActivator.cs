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
using Hangfire.Common;

namespace Hangfire
{
    public class JobActivator
    {
        private static JobActivator _current = new JobActivator();

        /// <summary>
        /// Gets or sets the current <see cref="JobActivator"/> instance 
        /// that will be used to activate jobs during performance.
        /// </summary>
        public static JobActivator Current
        {
            get { return _current; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _current = value;
            }
        }

        public virtual object ActivateJob(Type jobType)
        {
            return Activator.CreateInstance(jobType);
        }

        /// <summary>
        /// Override that allows the Job details to be passed in so that implementor 
        /// can retrieve more information about the job to properly create the given type
        /// </summary>
        /// <param name="jobType"></param>
        /// <param name="job">The job that is about to be created</param>
        /// <returns></returns>
        public virtual object ActivateJob(Type jobType, Job job)
        {
            return Activator.CreateInstance(jobType);
        }
    }
}
