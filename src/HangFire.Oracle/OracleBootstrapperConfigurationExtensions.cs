// // This file is part of Hangfire.
// // Copyright © 2013-2014 Sergey Odinokov.
// // 
// // Hangfire is free software: you can redistribute it and/or modify
// // it under the terms of the GNU Lesser General Public License as 
// // published by the Free Software Foundation, either version 3 
// // of the License, or any later version.
// // 
// // Hangfire is distributed in the hope that it will be useful,
// // but WITHOUT ANY WARRANTY; without even the implied warranty of
// // MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// // GNU Lesser General Public License for more details.
// // 
// // You should have received a copy of the GNU Lesser General Public 
// // License along with Hangfire. If not, see <http://www.gnu.org/licenses/>.

namespace Hangfire.Oracle
{
    public static class OracleBootstrapperConfigurationExtensions
    {
        /// <summary>
        /// Tells the bootstrapper to use SQL Server as a job storage,
        /// that can be accessed using the given connection string or 
        /// its name.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="nameOrConnectionString">Connection string or its name</param>
        public static OracleStorage UseOracleStorage(
            this IBootstrapperConfiguration configuration,
            string nameOrConnectionString)
        {
            var storage = new OracleStorage(nameOrConnectionString);
            configuration.UseStorage(storage);

            return storage;
        }
    }
}