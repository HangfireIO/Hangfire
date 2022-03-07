// This file is part of Hangfire. Copyright © 2015 Sergey Odinokov.
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
using System.Data.Common;
using Hangfire.Annotations;
using Hangfire.SqlServer;

// ReSharper disable once CheckNamespace
namespace Hangfire
{
    public static class SqlServerStorageExtensions
    {
        public static IGlobalConfiguration<SqlServerStorage> UseSqlServerStorage(
            [NotNull] this IGlobalConfiguration configuration,
            [NotNull] string nameOrConnectionString)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            if (nameOrConnectionString == null) throw new ArgumentNullException(nameof(nameOrConnectionString));

            var storage = new SqlServerStorage(nameOrConnectionString);
            return configuration.UseStorage(storage);
        }

        public static IGlobalConfiguration<SqlServerStorage> UseSqlServerStorage(
            [NotNull] this IGlobalConfiguration configuration,
            [NotNull] string nameOrConnectionString, 
            [NotNull] SqlServerStorageOptions options)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            if (nameOrConnectionString == null) throw new ArgumentNullException(nameof(nameOrConnectionString));
            if (options == null) throw new ArgumentNullException(nameof(options));

            var storage = new SqlServerStorage(nameOrConnectionString, options);
            return configuration.UseStorage(storage);
        }

        public static IGlobalConfiguration<SqlServerStorage> UseSqlServerStorage(
            [NotNull] this IGlobalConfiguration configuration,
            [NotNull] Func<DbConnection> connectionFactory)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            if (connectionFactory == null) throw new ArgumentNullException(nameof(connectionFactory));

            var storage = new SqlServerStorage(connectionFactory);
            return configuration.UseStorage(storage);
        }

        public static IGlobalConfiguration<SqlServerStorage> UseSqlServerStorage(
            [NotNull] this IGlobalConfiguration configuration,
            [NotNull] Func<DbConnection> connectionFactory,
            [NotNull] SqlServerStorageOptions options)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            if (connectionFactory == null) throw new ArgumentNullException(nameof(connectionFactory));
            if (options == null) throw new ArgumentNullException(nameof(options));

            var storage = new SqlServerStorage(connectionFactory, options);
            return configuration.UseStorage(storage);
        }
    }
}
