// This file is part of HangFire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// HangFire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// HangFire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with HangFire. If not, see <http://www.gnu.org/licenses/>.

namespace HangFire.SqlServer
{
    public static class SqlServerStorageConfigurationExtensions
    {
        public static SqlServerStorage UseSqlServerStorage(
            this IStartupConfiguration configuration,
            string nameOrConnectionString)
        {
            var storage = new SqlServerStorage(nameOrConnectionString);
            configuration.UseStorage(storage);

            return storage;
        }

        public static SqlServerStorage UseSqlServerStorage(
            this IStartupConfiguration configuration,
            string nameOrConnectionString,
            SqlServerStorageOptions options)
        {
            var storage = new SqlServerStorage(nameOrConnectionString, options);
            configuration.UseStorage(storage);

            return storage;
        }
    }
}
