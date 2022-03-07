// This file is part of Hangfire. Copyright © 2013-2014 Sergey Odinokov.
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
