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
    /// <summary>
    /// Provides the context for the Dashboard UI. This class serves as a base class for specific web application frameworks,
    /// such as ASP.NET Core or OWIN, and is accessible from different Dashboard UI request dispatchers (please see
    /// <see cref="IDashboardDispatcher"/>), like pages or other endpoints.
    /// </summary>
    /// <remarks>
    /// The <see cref="DashboardContext"/> class encapsulates the HTTP request and response details,
    /// along with settings and services necessary to process dashboard requests.
    /// It provides an abstraction that allows easy integration with various web frameworks by inheriting from this class
    /// and implementing the specific behavior required for those frameworks.
    /// </remarks>
    public abstract class DashboardContext
    {
        private readonly Lazy<bool> _isReadOnlyLazy;

        /// <summary>
        /// Initializes a new instance of the <see cref="DashboardContext"/> class.
        /// </summary>
        /// <param name="storage">The job storage used by the Dashboard UI.</param>
        /// <param name="options">The options for configuring the Dashboard UI.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="storage"/> or <paramref name="options"/> is null.
        /// </exception>
        protected DashboardContext([NotNull] JobStorage storage, [NotNull] DashboardOptions options)
        {
            Storage = storage ?? throw new ArgumentNullException(nameof(storage));
            Options = options ?? throw new ArgumentNullException(nameof(options));

            _isReadOnlyLazy = new Lazy<bool>(() => Options.IsReadOnlyFunc(this));
        }

        /// <summary>
        /// Gets the <see cref="JobStorage"/> instance used by the Dashboard UI.
        /// </summary>
        public JobStorage Storage { get; }

        /// <summary>
        /// Gets the <see cref="DashboardOptions"/> for configuring the Dashboard UI.
        /// </summary>
        public DashboardOptions Options { get; }

        /// <summary>
        /// Gets or sets the URI match information passed from the configured <c>pathTemplate</c>
        /// when defining a route in the <see cref="DashboardRoutes"/> class.
        /// </summary>
        public Match UriMatch { get; set; }

        /// <summary>
        /// Gets the <see cref="DashboardRequest"/> metadata.
        /// Used by request dispatchers (please see <see cref="IDashboardDispatcher"/>) to provide request information.
        /// </summary>
        public DashboardRequest Request { get; protected set; }

        /// <summary>
        /// Gets the <see cref="DashboardResponse"/> metadata.
        /// Used by request dispatchers (please see <see cref="IDashboardDispatcher"/>) to send response information.
        /// </summary>
        public DashboardResponse Response { get; protected set; }

        /// <summary>
        /// Gets a value indicating whether the Dashboard UI is in read-only mode to possibly
        /// hide elements that modify the <see cref="JobStorage"/> instance's data.
        /// </summary>
        public bool IsReadOnly => _isReadOnlyLazy.Value;

        /// <summary>
        /// Gets or sets the anti-forgery header value.
        /// </summary>
        public string AntiforgeryHeader { get; set; }

        /// <summary>
        /// Gets or sets the anti-forgery token value.
        /// </summary>
        public string AntiforgeryToken { get; set; }

        /// <summary>
        /// Gets the background job client for the current <see cref="JobStorage"/> instance.
        /// </summary>
        /// <returns>An instance of <see cref="IBackgroundJobClient"/>.</returns>
        public virtual IBackgroundJobClient GetBackgroundJobClient()
        {
            return new BackgroundJobClient(Storage);
        }

        /// <summary>
        /// Gets the recurring job manager for the current <see cref="JobStorage"/> instance.
        /// </summary>
        /// <returns>An instance of <see cref="IRecurringJobManager"/>.</returns>
        public virtual IRecurringJobManager GetRecurringJobManager()
        {
            return new RecurringJobManager(
                Storage,
                JobFilterProviders.Providers,
                Options.TimeZoneResolver ?? new DefaultTimeZoneResolver());
        }

        public virtual IRecurringJobManagerV2 GetRecurringJobManagerV2()
        {
            return (IRecurringJobManagerV2)GetRecurringJobManager();
        }
    }
}