// This file is part of Hangfire. Copyright © 2025 Hangfire OÜ.
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
using System.Collections.Generic;
using System.Data.Common;
using Hangfire.Annotations;

namespace Hangfire.SqlServer
{
    internal static class DbDataReaderExtensions
    {
        public static T ReadSingleOrDefaultAndProceed<T>([NotNull] this DbDataReader reader, [NotNull] Func<DbDataReader, T> mapper)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));

            if (!reader.Read()) return default;

            var result = mapper(reader);

            EnsureNoRowsRemainingOrThrow(reader);
            EnsureNextResultOrThrow(reader);

            return result;
        }

        public static T ReadSingleAndProceed<T>([NotNull] this DbDataReader reader, [NotNull] Func<DbDataReader, T> mapper)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));

            EnsureDataRowsAvailableOrThrow(reader);

            var result = mapper(reader);

            EnsureNoRowsRemainingOrThrow(reader);
            EnsureNextResultOrThrow(reader);

            return result;
        }

        public static List<T> ReadListAndProceed<T>([NotNull] this DbDataReader reader, [NotNull] Func<DbDataReader, T> mapper)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));

            var result = new List<T>();

            while (reader.Read())
            {
                result.Add(mapper(reader));
            }

            EnsureNextResultOrThrow(reader);

            return result;
        }

        public static T ReadSingleOrDefaultAndFinish<T>([NotNull] this DbDataReader reader, [NotNull] Func<DbDataReader, T> mapper)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));

            if (!reader.Read()) return default;

            var result = mapper(reader);

            EnsureNoRowsRemainingOrThrow(reader);
            EnsureNextResultDoesNotExist(reader);

            return result;
        }

        public static T ReadSingleAndFinish<T>([NotNull] this DbDataReader reader, [NotNull] Func<DbDataReader, T> mapper)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));

            EnsureDataRowsAvailableOrThrow(reader);

            var result = mapper(reader);

            EnsureNoRowsRemainingOrThrow(reader);
            EnsureNextResultDoesNotExist(reader);

            return result;
        }

        public static List<T> ReadListAndFinish<T>([NotNull] this DbDataReader reader, [NotNull] Func<DbDataReader, T> mapper)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));

            var result = new List<T>();

            while (reader.Read())
            {
                result.Add(mapper(reader));
            }

            EnsureNextResultDoesNotExist(reader);

            return result;
        }

        public static string GetRequiredString([NotNull] this DbDataReader reader, [CanBeNull] string name = null)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));

            var ordinal = name != null ? reader.GetOrdinal(name) : 0;
            return reader.GetString(ordinal);
        }

        public static string GetOptionalString([NotNull] this DbDataReader reader, [CanBeNull] string name = null)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));

            var ordinal = name != null ? reader.GetOrdinal(name) : 0;
            return !reader.IsDBNull(ordinal) ? reader.GetString(ordinal) : null;
        }

        public static DateTime GetRequiredDateTime([NotNull] this DbDataReader reader, [CanBeNull] string name = null)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));

            var ordinal = name != null ? reader.GetOrdinal(name) : 0;
            return reader.GetDateTime(ordinal);
        }

        public static DateTime? GetOptionalDateTime([NotNull] this DbDataReader reader, [CanBeNull] string name = null)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));

            var ordinal = name != null ? reader.GetOrdinal(name) : 0;
            return !reader.IsDBNull(ordinal) ? reader.GetDateTime(ordinal) : null;
        }

        public static T GetRequiredValue<T>([NotNull] this DbDataReader reader, [CanBeNull] string name = null)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));

            var ordinal = name != null ? reader.GetOrdinal(name) : 0;
            return DbCommandExtensions.ConvertValue<T>(reader.GetValue(ordinal));
        }

        public static T GetOptionalValue<T>([NotNull] this DbDataReader reader, [CanBeNull] string name = null)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));

            var ordinal = name != null ? reader.GetOrdinal(name) : 0;
            return !reader.IsDBNull(ordinal) ? DbCommandExtensions.ConvertValue<T>(reader.GetValue(ordinal)) : default;
        }

        private static void EnsureDataRowsAvailableOrThrow(DbDataReader reader)
        {
            if (!reader.Read())
            {
                throw new InvalidOperationException("No rows returned from SQL Server, while expecting at least one.");
            }
        }

        private static void EnsureNoRowsRemainingOrThrow(DbDataReader reader)
        {
            if (reader.Read())
            {
                throw new InvalidOperationException("Multiple rows returned from SQL Server, while expecting single or none.");
            }
        }

        private static void EnsureNextResultOrThrow(DbDataReader reader)
        {
            if (!reader.NextResult())
            {
                throw new InvalidOperationException("No next result set is found.");
            }
        }

        private static void EnsureNextResultDoesNotExist(DbDataReader reader)
        {
            if (reader.NextResult())
            {
                throw new InvalidOperationException("Unexpected next result set is found after finishing to read data.");
            }
        }
    }
}