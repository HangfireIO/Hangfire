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
using HangFire.Server.Performing;

namespace HangFire.Server.Filters
{
    /// <summary>
    /// Provides the context for the <see cref="IServerFilter.OnPerformed"/>
    /// method of the <see cref="IServerFilter"/> interface.
    /// </summary>
    public class PerformedContext : PerformContext
    {
        internal PerformedContext(
            PerformContext context, 
            bool canceled, 
            Exception exception)
            : base(context)
        {
            Canceled = canceled;
            Exception = exception;
        }

        /// <summary>
        /// Gets a value that indicates that this <see cref="PerformedContext"/>
        /// object was canceled.
        /// </summary>
        public bool Canceled { get; private set; }

        /// <summary>
        /// Gets an exception that occured during the performance of the job.
        /// </summary>
        public Exception Exception { get; private set; }

        /// <summary>
        /// Gets or sets a value that indicates that this <see cref="PerformedContext"/>
        /// object handles an exception occured during the performance of the job.
        /// </summary>
        public bool ExceptionHandled { get; set; }
    }
}