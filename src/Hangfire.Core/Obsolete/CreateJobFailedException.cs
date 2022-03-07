// This file is part of Hangfire. Copyright © 2013-2014 Sergey Odinokov.
// 
// Permission to use, copy, modify, and/or distribute this software for any
// purpose with or without fee is hereby granted.
// 
// THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
// REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY
// AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
// INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM
// LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR
// OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR
// PERFORMANCE OF THIS SOFTWARE.

using System;

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
        public CreateJobFailedException(string message, Exception inner) 
            : base(message, inner)
        {
        }
    }
}
