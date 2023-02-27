// This file is part of Hangfire. Copyright © 2013-2014 Hangfire OÜ.
// 
// Hangfire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// Hangfire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with Hangfire. If not, see <http://www.gnu.org/licenses/>.

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
