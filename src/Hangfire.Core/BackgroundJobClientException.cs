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
using Hangfire.Client;

#pragma warning disable 618 // Obsolete member

namespace Hangfire
{
    /// <summary>
    /// The exception that is thrown when an instance of the class that 
    /// implements the <see cref="IBackgroundJobClient"/> interface is unable
    /// to perform an operation due to an error.
    /// </summary>
#if !NETSTANDARD1_3
    [Serializable]
#endif
    public class BackgroundJobClientException : CreateJobFailedException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundJobClientException"/>
        /// class with a specified error message and a reference to the inner exception
        /// that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="inner">The exception that is the cause of this exception, not null.</param>
        public BackgroundJobClientException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
