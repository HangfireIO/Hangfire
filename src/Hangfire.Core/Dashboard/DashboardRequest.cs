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

using System.Collections.Generic;
using System.Threading.Tasks;
using Hangfire.Annotations;

// ReSharper disable RedundantNullnessAttributeWithNullableReferenceTypes
#nullable enable

namespace Hangfire.Dashboard
{
    /// <summary>
    /// Provides the request details for the Dashboard UI. This class serves as an abstraction for HTTP requests
    /// and is used within <see cref="IDashboardDispatcher"/> implementations to access request information.
    /// </summary>
    /// <remarks>
    /// The <see cref="DashboardRequest"/> class encapsulates the HTTP request details, providing properties to access
    /// the method, path, base path, IP addresses, and query or form values.
    /// This allows for a consistent way to handle requests across different web frameworks.
    /// </remarks>
    public abstract class DashboardRequest
    {
        /// <summary>
        /// Gets the HTTP method of the request like <c>"GET"</c> or <c>"POST"</c>, that can
        /// be checked for equality by using the <see cref="System.StringComparison.OrdinalIgnoreCase"/> comparer. 
        /// </summary>
        [NotNull]
        public abstract string Method { get; }

        /// <summary>
        /// Gets the request path for the current request that doesn't include the <see cref="DashboardOptions.PrefixPath"/>,
        /// like <c>"/jobs/enqueued"</c>.
        /// </summary>
        [NotNull]
        public abstract string Path { get; }

        /// <summary>
        /// Gets the base path for the request configured in the request middleware, usually useful
        /// to reconstruct full URIs like for link generation.
        /// </summary>
        [CanBeNull]
        public abstract string? PathBase { get; }

        /// <summary>
        /// Gets the local IP address from which the request originated.
        /// </summary>
        [CanBeNull]
        public abstract string? LocalIpAddress { get; }

        /// <summary>
        /// Gets the remote IP address from which the request originated.
        /// </summary>
        [CanBeNull]
        public abstract string? RemoteIpAddress { get; }

        /// <summary>
        /// Gets the value of a specific query string parameter.
        /// </summary>
        /// <param name="key">The key of the query string parameter.</param>
        /// <returns>The value of the query string parameter.</returns>
        [CanBeNull]
        public abstract string? GetQuery([NotNull] string key);

        /// <summary>
        /// Gets the values of a specific form parameter asynchronously, reading the request body if it's a form.
        /// </summary>
        /// <param name="key">The key of the form parameter.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the list of values for the form parameter.</returns>
        public abstract Task<IList<string>> GetFormValuesAsync([NotNull] string key);
    }
}