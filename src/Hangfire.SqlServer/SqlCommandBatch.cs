// This file is part of Hangfire. Copyright © 2017 Sergey Odinokov.
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
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;

namespace Hangfire.SqlServer
{
    internal class SqlCommandBatch : IDisposable
    {
        private readonly List<DbCommand> _commandList = new List<DbCommand>();
        private readonly SqlCommandSet _commandSet;
        private readonly int _defaultTimeout;

        public SqlCommandBatch(DbConnection connection, DbTransaction transaction, bool preferBatching)
        {
            Connection = connection;
            Transaction = transaction;

            if (connection is SqlConnection && SqlCommandSet.IsAvailable && preferBatching)
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

        public void Append(string commandText, params SqlCommandBatchParameter[] parameters)
        {
            var command = Connection.CreateCommand();
            command.CommandText = commandText;

            foreach (var parameter in parameters)
            {
                parameter.AddToCommand(command);
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

    internal class SqlCommandBatchParameter
    {
        public SqlCommandBatchParameter(string parameterName, DbType dbType, int? size = null)
        {
            ParameterName = parameterName;
            DbType = dbType;
            Size = size;
        }

        public string ParameterName { get; }
        public DbType DbType { get; }
        public int? Size { get; }
        public object Value { get; set; }

        public void AddToCommand(DbCommand command)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = ParameterName;
            parameter.DbType = DbType;

            if (Size.HasValue) parameter.Size = Size.Value;

            parameter.Value = Value;

            command.Parameters.Add(parameter);
        }
    }
}