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
using System.Data;
using System.Data.Common;

namespace Hangfire.SqlServer
{
    internal static class DbConnectionExtensions
    {
        public static DbCommand CreateCommand(
            this DbConnection connection,
            string text,
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
    }
}