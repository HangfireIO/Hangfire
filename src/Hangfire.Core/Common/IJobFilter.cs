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

namespace Hangfire.Common
{
    /// <summary>
    /// Defines members that specify the order of filters and 
    /// whether multiple filters are allowed.
    /// </summary>
    public interface IJobFilter
    {
        /// <summary>
        /// When implemented in a class, gets or sets a value 
        /// that indicates whether multiple filters are allowed.
        /// </summary>
        bool AllowMultiple { get; }

        /// <summary>
        /// When implemented in a class, gets the filter order.
        /// </summary>
        int Order { get; }
    }
}