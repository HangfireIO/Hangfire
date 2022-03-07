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

namespace Hangfire.Client
{
    /// <summary>
    /// Provides the context for the <see cref="IClientExceptionFilter.OnClientException"/>
    /// method of the <see cref="IClientExceptionFilter"/> interface.
    /// </summary>
    public class ClientExceptionContext : CreateContext
    {
        public ClientExceptionContext(CreateContext createContext, Exception exception)
            : base(createContext)
        {
            if (exception == null) throw new ArgumentNullException(nameof(exception));

            Exception = exception;
        }

        /// <summary>
        /// Gets an exception that occurred during the creation of the job.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// Gets or sets a value that indicates that this <see cref="ClientExceptionContext"/>
        /// object handles an exception occurred during the creation of the job.
        /// </summary>
        public bool ExceptionHandled { get; set; }
    }
}