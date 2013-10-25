using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

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

        protected CreateJobFailedException(
            SerializationInfo info,
            StreamingContext context) 
            : base(info, context)
        {
        }
    }
}
