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

using System;

namespace Hangfire.SqlServer
{
    public static class SqlServerBootstrapperConfigurationExtensions
    {
        /// <summary>
        /// Tells the bootstrapper to use SQL Server as a job storage,
        /// that can be accessed using the given connection string or 
        /// its name.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="nameOrConnectionString">Connection string or its name</param>
        [Obsolete("Please use `GlobalConfiguration.UseSqlServerStorage` instead. Will be removed in version 2.0.0.")]
        public static SqlServerStorage UseSqlServerStorage(
            this IBootstrapperConfiguration configuration,
            string nameOrConnectionString)
        {
            var storage = new SqlServerStorage(nameOrConnectionString);
            configuration.UseStorage(storage);

            return storage;
        }

        /// <summary>
        /// Tells the bootstrapper to use SQL Server as a job storage
        /// with the given options, that can be accessed using the specified
        /// connection string or its name.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="nameOrConnectionString">Connection string or its name</param>
        /// <param name="options">Advanced options</param>
        [Obsolete("Please use `GlobalConfiguration.UseSqlServerStorage` instead. Will be removed in version 2.0.0.")]
        public static SqlServerStorage UseSqlServerStorage(
            this IBootstrapperConfiguration configuration,
            string nameOrConnectionString,
            SqlServerStorageOptions options)
        {
            var storage = new SqlServerStorage(nameOrConnectionString, options);
            configuration.UseStorage(storage);

            return storage;
        }
    }
}
