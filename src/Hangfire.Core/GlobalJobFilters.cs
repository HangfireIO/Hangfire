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

using Hangfire.Common;

namespace Hangfire
{
    /// <summary>
    /// Represents the global filter collection.
    /// </summary>
    public static class GlobalJobFilters
    {
        static GlobalJobFilters()
        {
            // ReSharper disable once UseObjectOrCollectionInitializer
            Filters = new JobFilterCollection();

            // Filters should be added with the `Add` method call: some 
            // of them indirectly use `GlobalJobFilters.Filters` property, 
            // and it is null, when we are using collection initializer.
            Filters.Add(new CaptureCultureAttribute());
            Filters.Add(new AutomaticRetryAttribute());
            Filters.Add(new StatisticsHistoryAttribute());
            Filters.Add(new ContinuationsSupportAttribute());
        }

        /// <summary>
        /// Gets the global filter collection.
        /// </summary>
        public static JobFilterCollection Filters { get; }
    }
}
