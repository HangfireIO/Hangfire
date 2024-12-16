// This file is part of Hangfire. Copyright © 2024 Hangfire OÜ.
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using Hangfire.Annotations;

namespace Hangfire.SqlServer
{
    internal static class DbCommandExtensions
    {
        private static readonly ConcurrentDictionary<KeyValuePair<string, KeyValuePair<string, int>>, string> ExpandedQueries =
            new ConcurrentDictionary<KeyValuePair<string, KeyValuePair<string, int>>, string>();

        public static DbCommand Create(
            [NotNull] this DbConnection connection,
            [NotNull] string text,
            CommandType type = CommandType.Text,
            int? timeout = null)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (text == null) throw new ArgumentNullException(nameof(text));

            var command = connection.CreateCommand();
            command.CommandType = type;
            command.CommandText = text;

            if (timeout.HasValue)
            {
                command.CommandTimeout = timeout.Value;
            }

            return command;
        }

        public static DbCommand AddParameter(
            [NotNull] this DbCommand command,
            [NotNull] string parameterName,
            [CanBeNull] object value,
            DbType dbType,
            [CanBeNull] int? size = null)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (parameterName == null) throw new ArgumentNullException(nameof(parameterName));

            var parameter = AddParameterInternal(command, parameterName, dbType, size);
            parameter.Value = value ?? DBNull.Value;

            return command;
        }

        public static DbCommand AddReturnParameter(
            [NotNull] this DbCommand command,
            string parameterName,
            out DbParameter parameter,
            DbType dbType,
            int? size = null)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (parameterName == null) throw new ArgumentNullException(nameof(parameterName));

            parameter = AddParameterInternal(command, parameterName, dbType, size);
            parameter.Direction = ParameterDirection.ReturnValue;
            parameter.Value = DBNull.Value;

            return command;
        }

        public static DbCommand AddExpandedParameter<T>(
            [NotNull] this DbCommand command,
            [NotNull] string parameterName,
            [NotNull] T[] parameterValues,
            DbType parameterType,
            int? parameterSize = null)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (parameterName == null) throw new ArgumentNullException(nameof(parameterName));
            if (parameterValues == null) throw new ArgumentNullException(nameof(parameterValues));

            command.CommandText = ExpandedQueries.GetOrAdd(
                new KeyValuePair<string, KeyValuePair<string, int>>(command.CommandText, new KeyValuePair<string, int>(parameterName, parameterValues.Length)),
                static pair =>
                {
                    return pair.Key.Replace(
                        pair.Value.Key, 
                        "(" + String.Join(",", Enumerable.Range(0, pair.Value.Value).Select(i => pair.Value.Key + i.ToString(CultureInfo.InvariantCulture))) + ")");
                });

            for (var i = 0; i < parameterValues.Length; i++)
            {
                var parameter = AddParameterInternal(command, parameterName + i.ToString(CultureInfo.InvariantCulture), parameterType, parameterSize);
                parameter.Value = (object)parameterValues[i] ?? DBNull.Value;
            }

            return command;
        }

        public static T GetParameterValue<T>([NotNull] this DbParameter parameter)
        {
            if (parameter == null) throw new ArgumentNullException(nameof(parameter));

            switch (parameter.Value)
            {
                case null or DBNull: return default;
                case T typed: return typed;
                default:
                    var type = typeof(T);
                    type = Nullable.GetUnderlyingType(type) ?? type;
                    return (T)Convert.ChangeType(parameter.Value, type, CultureInfo.InvariantCulture);                    
            }
        }

        private static DbParameter AddParameterInternal(
            DbCommand command,
            string parameterName,
            DbType dbType,
            int? size)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = parameterName;
            parameter.DbType = dbType;

            if (size.HasValue) parameter.Size = size.Value;

            command.Parameters.Add(parameter);
            return parameter;
        }
    }
}