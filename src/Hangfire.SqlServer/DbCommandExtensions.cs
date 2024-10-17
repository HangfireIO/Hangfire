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
using System.Data;
using System.Data.Common;
using Hangfire.Annotations;

namespace Hangfire.SqlServer
{
    internal static class DbCommandExtensions
    {
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

            var parameter = command.CreateParameter();
            parameter.ParameterName = parameterName;
            parameter.DbType = dbType;
            parameter.Value = value ?? DBNull.Value;

            if (size.HasValue) parameter.Size = size.Value;

            command.Parameters.Add(parameter);
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

            parameter = command.CreateParameter();
            parameter.ParameterName = parameterName;
            parameter.DbType = dbType;
            parameter.Direction = ParameterDirection.ReturnValue;

            if (size.HasValue) parameter.Size = size.Value;

            command.Parameters.Add(parameter);
            return command;
        }
    }
}