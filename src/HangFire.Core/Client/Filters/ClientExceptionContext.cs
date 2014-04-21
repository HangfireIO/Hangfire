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

namespace HangFire.Client.Filters
{
    /// <summary>
    /// Provides the context for the <see cref="IClientExceptionFilter.OnClientException"/>
    /// method of the <see cref="IClientExceptionFilter"/> interface.
    /// </summary>
    public class ClientExceptionContext : CreateContext
    {
        internal ClientExceptionContext(CreateContext createContext, Exception exception)
            : base(createContext)
        {
            if (exception == null) throw new ArgumentNullException("exception");

            Exception = exception;
        }

        /// <summary>
        /// Gets an exception that occurred during the creation of the job.
        /// </summary>
        public Exception Exception { get; private set; }

        /// <summary>
        /// Gets or sets a value that indicates that this <see cref="ClientExceptionContext"/>
        /// object handles an exception occurred during the creation of the job.
        /// </summary>
        public bool ExceptionHandled { get; set; }
    }
}