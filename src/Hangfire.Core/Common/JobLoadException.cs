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

namespace Hangfire.Common
{
    /// <summary>
    /// The exception that is thrown when a job could not
    /// be loaded from the storage due to missing or incorrect 
    /// information about its type or method.
    /// </summary>
#if !NETSTANDARD1_3
    [Serializable]
#endif
    public class JobLoadException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JobLoadException"/>
        /// class with a given message and information about inner exception.
        /// </summary>
        public JobLoadException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}