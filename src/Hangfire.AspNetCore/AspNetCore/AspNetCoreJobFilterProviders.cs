using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Hangfire.Common;
using Hangfire.Annotations;

namespace Hangfire.AspNetCore
{
    /// <summary>
    /// Composite job filter provider for ASP.NET Core. Uses the following sources:
    /// 1) providers registered under <see cref="JobFilterProviders.Providers"/>
    ///    (includes <see cref="GlobalJobFilters.Filters"/> and <see cref="JobFilterAttribute"/>s from job class/method);
    /// 2) services of type <see cref="IJobFilter"/> from service container;
    /// </summary>
    internal class AspNetCoreJobFilterProviders : JobFilterProviderCollection
    {
        public AspNetCoreJobFilterProviders([NotNull] IServiceProvider services)
        {
            Add(JobFilterProviders.Providers);
            Add(new ServiceProviderJobFilterProvider(services));
        }

        /// <summary>
        /// Implementation of <see cref="IJobFilterProvider"/> backed by <see cref="IServiceProvider"/>
        /// </summary>
        private class ServiceProviderJobFilterProvider : IJobFilterProvider
        {
            private readonly IServiceProvider _services;

            public ServiceProviderJobFilterProvider([NotNull] IServiceProvider services)
            {
                if (services == null) throw new ArgumentNullException(nameof(services));

                _services = services;
            }

            /// <summary>
            /// Returns global job filters for all services of type <see cref="IJobFilter"/>
            /// </summary>
            /// <param name="job">Job to return filters for (currently not used)</param>
            /// <returns>Job filters from service container</returns>
            public IEnumerable<JobFilter> GetFilters(Job job)
            {
                return _services.GetServices<IJobFilter>()
                                .Select(x => new JobFilter(x, JobFilterScope.Global, null));
            }
        }
    }
}