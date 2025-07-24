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
using System.IO;
using System.Threading.Tasks;
using Hangfire.Annotations;

namespace Hangfire.Dashboard
{
    /// <summary>
    /// Provides the response details for the Dashboard UI. This class serves as an abstraction for HTTP responses
    /// and is used within <see cref="IDashboardDispatcher"/> implementations to send response information.
    /// </summary>
    /// <remarks>
    /// The <see cref="DashboardResponse"/> class encapsulates the HTTP response details, providing properties and methods
    /// to set the content type, status code, body, and expiration of the response.
    /// This allows for a consistent way to handle responses across different web frameworks.
    /// </remarks>
    public abstract class DashboardResponse
    {
        /// <summary>
        /// Gets or sets the content type of the response like <c>"application/json"</c>.
        /// </summary>
        /// <value>The content type of the response.</value>
        [NotNull]
        public abstract string ContentType { get; set; }

        /// <summary>
        /// Gets or sets the HTTP status code of the response like <c>200</c>.
        /// </summary>
        /// <value>The status code of the response.</value>
        public abstract int StatusCode { get; set; }

        /// <summary>
        /// Gets the response body stream, most of the time it's better to use the
        /// <see cref="WriteAsync"/> method instead for text data.
        /// </summary>
        /// <value>The response body stream.</value>
        [NotNull]
        public abstract Stream Body { get; }

        /// <summary>
        /// Sets the expiration time for the response.
        /// </summary>
        /// <param name="value">The expiration time, or <c>null</c> to remove the expiration header.</param>
        public abstract void SetExpire(DateTimeOffset? value);

        /// <summary>
        /// Writes the specified text to the response body asynchronously.
        /// </summary>
        /// <param name="text">The text to write to the response body.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public abstract Task WriteAsync([NotNull] string text);
    }
}