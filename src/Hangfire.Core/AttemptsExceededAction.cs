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

namespace Hangfire
{
    /// <summary>
    /// Specifies a candidate state for a background job that will be chosen
    /// by the <see cref="AutomaticRetryAttribute"/> filter after exceeding
    /// the number of retry attempts.
    /// </summary>
    public enum AttemptsExceededAction
    {
        /// <summary>
        /// Background job will be moved to the <see cref="States.FailedState"/>.
        /// </summary>
        Fail = 0,

        /// <summary>
        /// Background job will be moved to the <see cref="States.DeletedState"/>.
        /// </summary>
        Delete
    }
}