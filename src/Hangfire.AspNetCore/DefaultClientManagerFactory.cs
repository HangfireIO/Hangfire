// This file is part of Hangfire. Copyright © 2021 Sergey Odinokov.
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
using Hangfire.Annotations;
using Hangfire.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Hangfire
{
    internal sealed class DefaultClientManagerFactory : IBackgroundJobClientFactory, IRecurringJobManagerFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public DefaultClientManagerFactory([NotNull] IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public IBackgroundJobClient GetClient(JobStorage storage)
        {
            if (HangfireServiceCollectionExtensions.GetInternalServices(_serviceProvider, out var factory, out var stateChanger, out _))
            {
                return new BackgroundJobClient(storage, factory, stateChanger);
            }

            return new BackgroundJobClient(
                storage,
                _serviceProvider.GetRequiredService<IJobFilterProvider>());
        }

        public IRecurringJobManager GetManager(JobStorage storage)
        {
            if (HangfireServiceCollectionExtensions.GetInternalServices(_serviceProvider, out var factory, out _, out _))
            {
                return new RecurringJobManager(
                    storage,
                    factory,
                    _serviceProvider.GetRequiredService<ITimeZoneResolver>());
            }

            return new RecurringJobManager(
                storage,
                _serviceProvider.GetRequiredService<IJobFilterProvider>(),
                _serviceProvider.GetRequiredService<ITimeZoneResolver>());
        }
    }
}