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
using System.Data.SqlClient;
using System.Linq.Expressions;
using System.Reflection;

namespace Hangfire.SqlServer
{
    internal class SqlCommandSet : IDisposable
    {
        public  static readonly bool IsAvailable;

        private static readonly Type SqlCommandSetType = null;
        private readonly object _instance;

        private static readonly Action<object, SqlConnection> SetConnection = null;
        private static readonly Action<object, SqlTransaction> SetTransaction = null;
        private static readonly Func<object, SqlCommand> GetBatchCommand = null;
        private static readonly Action<object, SqlCommand> AppendMethod = null;
        private static readonly Func<object, int> ExecuteNonQueryMethod = null;
        private static readonly Action<object> DisposeMethod = null;

        static SqlCommandSet()
        {
            try
            {
                var typeAssembly = typeof(SqlCommand).GetTypeInfo().Assembly;
                var version = typeAssembly.GetName().Version;

                if (Version.Parse("4.0.0.0") < version && version < Version.Parse("4.6.0.0"))
                {
                    // .NET Core version of the System.Data.SqlClient package below 4.7.0 (which
                    // has assembly version 4.6.0.0) doesn't properly implement the SqlCommandSet
                    // class, throwing the following exception in run-time:
                    // ArgumentException: Specified parameter name 'Parameter1' is not valid.
                    // GitHub Issue: https://github.com/dotnet/corefx/issues/29391

                    IsAvailable = false;
                    return;
                }

                SqlCommandSetType = typeAssembly.GetType("System.Data.SqlClient.SqlCommandSet");

                if (SqlCommandSetType == null) return;

                var p = Expression.Parameter(typeof(object));
                var converted = Expression.Convert(p, SqlCommandSetType);

                var connectionParameter = Expression.Parameter(typeof(SqlConnection));
                var transactionParameter = Expression.Parameter(typeof(SqlTransaction));
                var commandParameter = Expression.Parameter(typeof(SqlCommand));

                SetConnection = Expression.Lambda<Action<object, SqlConnection>>(Expression.Call(converted, "set_Connection", null, connectionParameter), p, connectionParameter).Compile();
                SetTransaction = Expression.Lambda<Action<object, SqlTransaction>>(Expression.Call(converted, "set_Transaction", null, transactionParameter), p, transactionParameter).Compile();
                GetBatchCommand = Expression.Lambda<Func<object, SqlCommand>>(Expression.Call(converted, "get_BatchCommand", null), p).Compile();
                AppendMethod = Expression.Lambda<Action<object, SqlCommand>>(Expression.Call(converted, "Append", null, commandParameter), p, commandParameter).Compile();
                ExecuteNonQueryMethod = Expression.Lambda<Func<object, int>>(Expression.Call(converted, "ExecuteNonQuery", null), p).Compile();
                DisposeMethod = Expression.Lambda<Action<object>>(Expression.Call(converted, "Dispose", null), p).Compile();

                IsAvailable = true;
            }
            catch (Exception)
            {
                IsAvailable = false;
            }
        }

        public SqlCommandSet()
        {
            if (!IsAvailable)
            {
                throw new PlatformNotSupportedException("SqlCommandSet is not supported on this platform, use regular commands instead");
            }

            _instance = Activator.CreateInstance(SqlCommandSetType, true);
        }

        public SqlConnection Connection
        {
            set { SetConnection(_instance, value); }
        }

        public SqlTransaction Transaction
        {
            set { SetTransaction(_instance, value); }
        }

        public SqlCommand BatchCommand => GetBatchCommand(_instance);
        public int CommandCount { get; private set; }

        public void Append(SqlCommand command)
        {
            AppendMethod(_instance, command);
            CommandCount++;
        }

        public int ExecuteNonQuery()
        {
            if (CommandCount == 0)
            {
                return 0;
            }

            return ExecuteNonQueryMethod(_instance);
        }

        public void Dispose()
        {
            DisposeMethod(_instance);
        }
    }
}