// This file is part of Hangfire.
// Copyright © 2013-2014 Sergey Odinokov.
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
using Hangfire.Dashboard;

namespace Hangfire
{
    /// <summary>
    /// Represents a configuration class for Hangfire components that
    /// is being used by the <see cref="OwinBootstrapper"/> class.
    /// </summary>
    public interface IBootstrapperConfiguration
    {
        /// <summary>
        /// Tells bootstrapper to pass the given collection of filters
        /// to the dashboard middleware to authorize dashboard requests. 
        /// Previous calls to this method are ignored. Empty array 
        /// enables access for all users.
        /// </summary>
        /// <param name="filters">Authorization filters</param>
        void UseAuthorizationFilters(params IAuthorizationFilter[] filters);

        /// <summary>
        /// Tells bootstrapper to register the given job filter globally.
        /// </summary>
        /// <param name="filter">Job filter instance</param>
        void UseFilter(object filter);

        /// <summary>
        /// Tells bootstrapper to map the dashboard middleware to the
        /// given path in the OWIN pipeline. 
        /// </summary>
        /// <param name="path">Dashboard path, '/hangfire' by default</param>
        void UseDashboardPath(string path);

        /// <summary>
        /// Tells bootstrapper to register the given instance of the
        /// <see cref="JobStorage"/> class globally.
        /// </summary>
        /// <param name="storage">Job storage</param>
        void UseStorage(JobStorage storage);

        /// <summary>
        /// Tells bootstrapper to register the given instance of the
        /// <see cref="JobActivator"/> class globally.
        /// </summary>
        /// <param name="activator">Job storage</param>
        void UseActivator(JobActivator activator);

        /// <summary>
        /// Tells bootstrapper to start the given job server on application
        /// start, and stop it automatically on application shutdown request.
        /// </summary>
        /// <param name="server">Job server</param>
        void UseServer(Func<BackgroundJobServer> server);
    }
}