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
using System.Collections.Concurrent;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Hangfire.SqlServer
{
    internal class SqlCommandSet : IDisposable
    {
        private static readonly ConcurrentDictionary<Assembly, Type> SqlCommandSetType = new ConcurrentDictionary<Assembly, Type>();
        private static readonly ConcurrentDictionary<Type, Action<object, DbConnection>> SetConnection = new ConcurrentDictionary<Type, Action<object, DbConnection>>();
        private static readonly ConcurrentDictionary<Type, Action<object, DbTransaction>> SetTransaction = new ConcurrentDictionary<Type, Action<object, DbTransaction>>();
        private static readonly ConcurrentDictionary<Type, Func<object, DbCommand>> GetBatchCommand = new ConcurrentDictionary<Type, Func<object, DbCommand>>();
        private static readonly ConcurrentDictionary<Type, PropertyInfo> BatchCommandProperty = new ConcurrentDictionary<Type, PropertyInfo>();
        private static readonly ConcurrentDictionary<Type, Action<object, DbCommand>> AppendMethod = new ConcurrentDictionary<Type, Action<object, DbCommand>>();
        private static readonly ConcurrentDictionary<Type, Func<object, int>> ExecuteNonQueryMethod = new ConcurrentDictionary<Type, Func<object, int>>();
        private static readonly ConcurrentDictionary<Type, Action<object>> DisposeMethod = new ConcurrentDictionary<Type, Action<object>>();

        private readonly object _instance;

        private readonly Action<object, DbConnection> _setConnection;
        private readonly Action<object, DbTransaction> _setTransaction;
        private readonly Func<object, DbCommand> _getBatchCommand;
        private readonly Action<object, DbCommand> _appendMethod;
        private readonly Func<object, int> _executeNonQueryMethod;
        private readonly Action<object> _disposeMethod;

        public SqlCommandSet(DbConnection connection)
        {
            Type sqlCommandSetType;
            try
            {
                sqlCommandSetType = SqlCommandSetType.GetOrAdd(connection.GetType().GetTypeInfo().Assembly, sqlClientAssembly =>
                {
                    var assemblyName = sqlClientAssembly.GetName();
                    var version = assemblyName.Version;

                    if (assemblyName.Name == "System.Data.SqlClient" && Version.Parse("4.0.0.0") < version && version < Version.Parse("4.6.0.0"))
                    {
                        // .NET Core version of the System.Data.SqlClient package below 4.7.0 (which
                        // has assembly version 4.6.0.0) doesn't properly implement the SqlCommandSet
                        // class, throwing the following exception in run-time:
                        // ArgumentException: Specified parameter name 'Parameter1' is not valid.
                        // GitHub Issue: https://github.com/dotnet/corefx/issues/29391
                        throw new NotSupportedException(".NET Core version of the System.Data.SqlClient package below 4.7.0 (which has assembly version 4.6.0.0) doesn't properly implement the SqlCommandSet class.");
                    }

                    var type = sqlClientAssembly.GetTypes().FirstOrDefault(x => x.Name == "SqlCommandSet");
                    if (type == null)
                    {
                        throw new TypeLoadException($"Could not load type 'SqlCommandSet' from assembly '{sqlClientAssembly}'.");
                    }

                    return type;
                });

                _setConnection = SetConnection.GetOrAdd(sqlCommandSetType, type =>
                {
                    var p = Expression.Parameter(typeof(object));
                    var converted = Expression.Convert(p, type);
                    var connectionParameter = Expression.Parameter(typeof(DbConnection));
                    var connectionProperty = type.GetProperty("Connection", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ?? throw new MissingMemberException($"Property '{type.FullName}.Connection' not found.");
                    return Expression.Lambda<Action<object, DbConnection>>(Expression.Assign(Expression.Property(converted, connectionProperty), Expression.Convert(connectionParameter, connectionProperty.PropertyType)), p, connectionParameter).Compile();
                });
                _setTransaction = SetTransaction.GetOrAdd(sqlCommandSetType, type =>
                {
                    var p = Expression.Parameter(typeof(object));
                    var converted = Expression.Convert(p, type);
                    var transactionParameter = Expression.Parameter(typeof(DbTransaction));
                    var transactionProperty = type.GetProperty("Transaction", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ?? throw new MissingMemberException($"Property '{type.FullName}.Transaction' not found.");
                    return Expression.Lambda<Action<object, DbTransaction>>(Expression.Assign(Expression.Property(converted, transactionProperty), Expression.Convert(transactionParameter, transactionProperty.PropertyType)), p, transactionParameter).Compile();
                });
                var batchCommandProperty = BatchCommandProperty.GetOrAdd(sqlCommandSetType, type =>
                {
                    return type.GetProperty("BatchCommand", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ?? throw new MissingMemberException($"Property '{type.FullName}.BatchCommand' not found.");
                });
                _getBatchCommand = GetBatchCommand.GetOrAdd(sqlCommandSetType, type =>
                {
                    var p = Expression.Parameter(typeof(object));
                    var converted = Expression.Convert(p, type);
                    return Expression.Lambda<Func<object, DbCommand>>(Expression.Property(converted, batchCommandProperty), p).Compile();
                });
                _appendMethod = AppendMethod.GetOrAdd(sqlCommandSetType, type =>
                {
                    var p = Expression.Parameter(typeof(object));
                    var converted = Expression.Convert(p, type);
                    var batchCommandParameter = Expression.Parameter(typeof(DbCommand));
                    return Expression.Lambda<Action<object, DbCommand>>(Expression.Call(converted, "Append", null, Expression.Convert(batchCommandParameter, batchCommandProperty.PropertyType)), p, batchCommandParameter).Compile();
                });
                _executeNonQueryMethod = ExecuteNonQueryMethod.GetOrAdd(sqlCommandSetType, type =>
                {
                    var p = Expression.Parameter(typeof(object));
                    var converted = Expression.Convert(p, type);
                    return Expression.Lambda<Func<object, int>>(Expression.Call(converted, "ExecuteNonQuery", null), p).Compile();
                });
                _disposeMethod = DisposeMethod.GetOrAdd(sqlCommandSetType, type =>
                {
                    var p = Expression.Parameter(typeof(object));
                    var converted = Expression.Convert(p, type);
                    return Expression.Lambda<Action<object>>(Expression.Call(converted, "Dispose", null), p).Compile();
                });
            }
            catch (Exception exception) when (exception.IsCatchableExceptionType())
            {
                throw new NotSupportedException($"SqlCommandSet for {connection.GetType().FullName} is not supported, use regular commands instead", exception);
            }

            _instance = Activator.CreateInstance(sqlCommandSetType, true);
        }

        public DbConnection Connection
        {
            set => _setConnection(_instance, value);
        }

        public DbTransaction Transaction
        {
            set => _setTransaction(_instance, value);
        }

        public DbCommand BatchCommand => _getBatchCommand(_instance);
        public int CommandCount { get; private set; }

        public void Append(DbCommand command)
        {
            _appendMethod(_instance, command);
            CommandCount++;
        }

        public int ExecuteNonQuery()
        {
            if (CommandCount == 0)
            {
                return 0;
            }

            return _executeNonQueryMethod(_instance);
        }

        public void Dispose()
        {
            _disposeMethod(_instance);
        }
    }
}