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

using System.Threading.Tasks;
using Hangfire.Annotations;

// ReSharper disable RedundantNullnessAttributeWithNullableReferenceTypes
#nullable enable

namespace Hangfire.Dashboard
{
    /// <summary>
    /// Defines the method for dispatching requests within the Dashboard UI.
    /// Implementations of this interface handle incoming requests to the dashboard and produce appropriate responses.
    /// </summary>
    /// <remarks>
    /// The <see cref="IDashboardDispatcher"/> interface is used to process requests in the Dashboard UI.
    /// Implement this interface to handle custom routes and manage request processing logic.
    /// 
    /// To register custom dispatchers, use the <see cref="DashboardRoutes"/> class. The <c>DashboardRoutes.Routes</c>
    /// property allows for adding new routes that the dashboard will use to dispatch requests to custom handlers.
    /// </remarks>
    /// <seealso cref="DashboardContext"/>
    /// <seealso cref="DashboardRoutes"/>
    public interface IDashboardDispatcher
    {
        /// <summary>
        /// Processes the request within the provided <see cref="DashboardContext"/>.
        /// </summary>
        /// <param name="context">The context for the current dashboard request, containing information about the request and response.</param>
        /// <returns>A task that represents the asynchronous dispatch operation.</returns>
        Task Dispatch([NotNull] DashboardContext context);
    }
}