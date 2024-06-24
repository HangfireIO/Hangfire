// This file is part of Hangfire. Copyright © 2017 Hangfire OÜ.
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
            [NotNull] object value,
            DbType dbType,
            [CanBeNull] int? size = null)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (parameterName == null) throw new ArgumentNullException(nameof(parameterName));
            if (value == null) throw new ArgumentNullException(nameof(value));

            var parameter = command.CreateParameter();
            parameter.ParameterName = parameterName;
            parameter.DbType = dbType;
            parameter.Value = value;

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

    internal sealed class SqlCommandBatch : IDisposable
    {
        private readonly List<DbCommand> _commandList = new List<DbCommand>();
        private readonly SqlCommandSet _commandSet;
        private readonly int _defaultTimeout;

        public SqlCommandBatch(DbConnection connection, DbTransaction transaction, bool preferBatching)
        {
            Connection = connection;
            Transaction = transaction;

            if (preferBatching)
            {
                try
                {
                    _commandSet = new SqlCommandSet(connection);
                    _defaultTimeout = _commandSet.BatchCommand.CommandTimeout;
                }
                catch (Exception ex) when (ex.IsCatchableExceptionType())
                {
                    _commandSet = null;
                }
            }
        }

        public DbConnection Connection { get; }
        public DbTransaction Transaction { get; }

        public int? CommandTimeout { get; set; }
        public int? CommandBatchMaxTimeout { get; set; }

        public void Dispose()
        {
            foreach (var command in _commandList)
            {
                command.Dispose();
            }

            _commandSet?.Dispose();
        }

        public void Append(DbCommand command)
        {
            if (_commandSet != null)
            {
                _commandSet.Append(command);
            }
            else
            {
                _commandList.Add(command);
            }
        }

        public void ExecuteNonQuery()
        {
            if (_commandSet != null && _commandSet.CommandCount > 0)
            {
                _commandSet.Connection = Connection;
                _commandSet.Transaction = Transaction;

                var batchTimeout = CommandTimeout ?? _defaultTimeout;

                if (batchTimeout > 0)
                {
                    batchTimeout = batchTimeout * _commandSet.CommandCount;

                    if (CommandBatchMaxTimeout.HasValue)
                    {
                        batchTimeout = Math.Min(CommandBatchMaxTimeout.Value, batchTimeout);
                    }
                }

                _commandSet.BatchCommand.CommandTimeout = batchTimeout;
                _commandSet.ExecuteNonQuery();
            }

            foreach (var command in _commandList)
            {
                command.Connection = Connection;
                command.Transaction = Transaction;

                if (CommandTimeout.HasValue)
                {
                    command.CommandTimeout = CommandTimeout.Value;
                }

                command.ExecuteNonQuery();
            }
        }
    }
}