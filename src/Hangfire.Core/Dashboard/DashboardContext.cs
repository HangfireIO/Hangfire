// This file is part of Hangfire. Copyright © 2016 Hangfire OÜ.
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

using System;
using System.Text.RegularExpressions;
using Hangfire.Annotations;
using Hangfire.Common;

namespace Hangfire.Dashboard
{
    public abstract class DashboardContext
    {
        private readonly Lazy<bool> _isReadOnlyLazy;

        protected DashboardContext([NotNull] JobStorage storage, [NotNull] DashboardOptions options)
        {
            if (storage == null) throw new ArgumentNullException(nameof(storage));
            if (options == null) throw new ArgumentNullException(nameof(options));

            Storage = storage;
            Options = options;
            _isReadOnlyLazy = new Lazy<bool>(() => options.IsReadOnlyFunc(this));
        }

        public JobStorage Storage { get; }
        public DashboardOptions Options { get; }

        public Match UriMatch { get; set; }
        
        public DashboardRequest Request { get; protected set; }
        public DashboardResponse Response { get; protected set; }

        public bool IsReadOnly => _isReadOnlyLazy.Value;

        public string AntiforgeryHeader { get; set; }
        public string AntiforgeryToken { get; set; }

        public virtual IBackgroundJobClient GetBackgroundJobClient()
        {
            return new BackgroundJobClient(Storage);
        }

        public virtual IRecurringJobManager GetRecurringJobManager()
        {
            return new RecurringJobManager(
                Storage,
                JobFilterProviders.Providers,
                Options.TimeZoneResolver ?? new DefaultTimeZoneResolver());
        }
    }
}