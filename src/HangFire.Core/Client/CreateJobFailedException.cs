// This file is part of HangFire.
// Copyright © 2013-2014 Sergey Odinokov.
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

namespace HangFire.Client
{
    /// <summary>
    /// The exception that is thrown when a <see cref="JobClient"/> class instance
    /// could not create a job due to another exception was thrown.
    /// </summary>
    [Serializable]
    public class CreateJobFailedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateJobFailedException"/>
        /// class with a specified error message and a reference to the
        /// inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="inner">The exception that is the cause of this exception, not null.</param>
        public CreateJobFailedException(string message, Exception inner) 
            : base(message, inner)
        {
        }
    }
}
