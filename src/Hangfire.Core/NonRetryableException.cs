using System;

namespace Hangfire
{
    /// <summary>
    /// The exception that is thrown from a job that wishes to prevent automatic retries.
    /// </summary>
    public class NonRetryableException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NonRetryableException"/>
        /// class with a specified error message and a reference to the inner exception
        /// that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public NonRetryableException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NonRetryableException"/>
        /// class with a specified error message and a reference to the inner exception
        /// that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of this exception, not null.</param>
        public NonRetryableException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
