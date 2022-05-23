// This file is part of Hangfire. Copyright © 2013-2014 Hangfire OÜ.
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

namespace Hangfire.Server
{
    /// <summary>
    /// Provides the context for the <see cref="IServerFilter.OnPerformed"/>
    /// method of the <see cref="IServerFilter"/> interface.
    /// </summary>
    public class PerformedContext : PerformContext
    {
        public PerformedContext(
            PerformContext context,
            object result,
            bool canceled,
            Exception exception)
            : base(context)
        {
            Result = result;
            Canceled = canceled;
            Exception = exception;
        }

        /// <summary>
        /// Gets a value that was returned by the job.
        /// </summary>
        public object Result { get; }

        /// <summary>
        /// Gets a value that indicates that this <see cref="PerformedContext"/>
        /// object was canceled.
        /// </summary>
        public bool Canceled { get; }

        /// <summary>
        /// Gets an exception that occurred during the performance of the job.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// Gets or sets a value that indicates that this <see cref="PerformedContext"/>
        /// object handles an exception occurred during the performance of the job.
        /// </summary>
        public bool ExceptionHandled { get; set; }
    }
}