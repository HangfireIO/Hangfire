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
using HangFire.Client;
using HangFire.Common;
using HangFire.States;

namespace HangFire
{
    /// <summary>
    /// Represents a HangFire Client API. Contains methods related
    /// to the job creation feature. See the default implementation
    /// in the <see cref="BackgroundJobClient"/> class.
    /// </summary>
    public interface IBackgroundJobClient : IDisposable
    {
        /// <summary>
        /// Creates a given job in a specified state in the storage.
        /// </summary>
        /// 
        /// <param name="job">Background job that will be created in a storage.</param>
        /// <param name="state">The initial state of the job.</param>
        /// <returns>The unique identifier of the created job.</returns>
        /// 
        /// <exception cref="ArgumentNullException"><paramref name="job"/> argument is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="state"/> argument is null.</exception>
        /// <exception cref="CreateJobFailedException">Job creation has been failed due to inner exception.</exception>
        string Create(Job job, IState state);

        /// <summary>
        /// Gets the current job storage that is being used by this 
        /// <see cref="IBackgroundJobClient"/> instance.
        /// </summary>
        JobStorage Storage { get; }
    }
}