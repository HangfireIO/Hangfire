// This file is part of Hangfire.
// Copyright © 2017 Sergey Odinokov.
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
using System.Data.SqlClient;

namespace Hangfire.SqlServer
{
    internal class SqlCommandBatch : IDisposable
    {
        private readonly List<DbCommand> _commandList = new List<DbCommand>();
        private readonly SqlCommandSet _commandSet;
        private readonly int _defaultTimeout;

        public SqlCommandBatch(bool preferBatching)
        {
            if (SqlCommandSet.IsAvailable && preferBatching)
            {
                try
                {
                    _commandSet = new SqlCommandSet();
                    _defaultTimeout = _commandSet.BatchCommand.CommandTimeout;
                }
                catch (Exception)
                {
                    _commandSet = null;
                }
            }
        }

        public DbConnection Connection { get; set; }
        public DbTransaction Transaction { get; set; }

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

        public void Append(string commandText, params SqlParameter[] parameters)
        {
            var command = new SqlCommand(commandText);

            foreach (var parameter in parameters)
            {
                command.Parameters.Add(parameter);
            }

            Append(command);
        }

        public void Append(DbCommand command)
        {
            if (_commandSet != null && command is SqlCommand)
            {
                _commandSet.Append((SqlCommand)command);
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
                _commandSet.Connection = Connection as SqlConnection;
                _commandSet.Transaction = Transaction as SqlTransaction;

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