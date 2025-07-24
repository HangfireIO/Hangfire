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
using System.Runtime.Serialization;
using Hangfire.Annotations;

// ReSharper disable once CheckNamespace
namespace Hangfire.Client
{
    /// <summary>
    /// The exception that is thrown when a <see cref="BackgroundJobClient"/> class instance
    /// could not create a job due to another exception was thrown.
    /// </summary>
    [Obsolete("Please use the `BackgroundJobClientException` instead. Will be removed in 2.0.0.")]
#if !NETSTANDARD1_3
    [Serializable]
#endif
    public class CreateJobFailedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateJobFailedException"/>
        /// class with a specified error message and a reference to the
        /// inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="inner">The exception that is the cause of this exception, not null.</param>
        public CreateJobFailedException([CanBeNull] string? message, [CanBeNull] Exception? inner) 
            : base(message, inner)
        {
        }
        
#if !NETSTANDARD1_3
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateJobFailedException"/> class
        /// with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected CreateJobFailedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }
}
