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

namespace Hangfire.Client
{
    /// <summary>
    /// Defines methods that are required for a client filter.
    /// </summary>
    public interface IClientFilter
    {
        /// <summary>
        /// Called before the creation of the job. 
        /// </summary>
        /// <param name="filterContext">The filter context.</param>
        void OnCreating(CreatingContext filterContext);

        /// <summary>
        /// Called after the creation of the job.
        /// </summary>
        /// <param name="filterContext">The filter context.</param>
        void OnCreated(CreatedContext filterContext);
    }
}