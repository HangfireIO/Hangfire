// This file is part of Hangfire. Copyright © 2016 Sergey Odinokov.
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