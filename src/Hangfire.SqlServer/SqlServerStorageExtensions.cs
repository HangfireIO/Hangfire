﻿// This file is part of Hangfire.
// Copyright © 2015 Sergey Odinokov.
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
            if (configuration == null) throw new ArgumentNullException("configuration");
            if (nameOrConnectionString == null) throw new ArgumentNullException("nameOrConnectionString");

            var storage = new SqlServerStorage(nameOrConnectionString);
            return configuration.UseStorage(storage);
        }

        public static IGlobalConfiguration<SqlServerStorage> UseSqlServerStorage(
            [NotNull] this IGlobalConfiguration configuration,
            [NotNull] string nameOrConnectionString, 
            [NotNull] SqlServerStorageOptions options)
        {
            if (configuration == null) throw new ArgumentNullException("configuration");
            if (nameOrConnectionString == null) throw new ArgumentNullException("nameOrConnectionString");
            if (options == null) throw new ArgumentNullException("options");

            var storage = new SqlServerStorage(nameOrConnectionString, options);
            return configuration.UseStorage(storage);
        }
    }
}
