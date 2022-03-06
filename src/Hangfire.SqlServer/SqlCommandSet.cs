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