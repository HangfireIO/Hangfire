// This file is part of Hangfire. Copyright © 2013-2014 Hangfire OÜ.
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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Dapper;
using Hangfire.Annotations;
using Hangfire.Storage;

namespace Hangfire.SqlServer
{
    public class SqlServerDistributedLock : IDisposable
    {
        private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(1);

        private const string LockMode = "Exclusive";
        private const string LockOwner = "Session";

        // Connections to SQL Azure Database that are idle for 30 minutes 
        // or longer will be terminated. And since we are using separate
        // connection for a distributed lock, we'd like to prevent Resource
        // Governor from terminating it.
        private static readonly TimeSpan KeepAliveInterval = TimeSpan.FromMinutes(1);

        private static readonly IDictionary<int, string> LockErrorMessages
            = new Dictionary<int, string>
            {
                { -1, "The lock request timed out" },
                { -2, "The lock request was canceled" },
                { -3, "The lock request was chosen as a deadlock victim" },
                { -999, "Indicates a parameter validation or other call error" }
            };

        private static readonly ThreadLocal<Dictionary<string, int>> AcquiredLocks
            = new ThreadLocal<Dictionary<string, int>>(static () => new Dictionary<string, int>()); 

        private DbConnection _connection;
        private readonly SqlServerStorage _storage;
        private readonly string _resource;
        private readonly Timer _timer;
        private readonly object _lockObject = new object();

        private bool _completed;

        [Obsolete("Don't use this class directly, use SqlServerConnection.AcquireDistributedLock instead as it provides better safety. Will be removed in 2.0.0.")]
        [SuppressMessage("Performance", "CA1854:Prefer the \'IDictionary.TryGetValue(TKey, out TValue)\' method")]
        public SqlServerDistributedLock([NotNull] SqlServerStorage storage, [NotNull] string resource, TimeSpan timeout)
        {
            if (storage == null) throw new ArgumentNullException(nameof(storage));
            if (String.IsNullOrEmpty(resource)) throw new ArgumentNullException(nameof(resource));

            _storage = storage;
            _resource = resource;

            if (!AcquiredLocks.Value.ContainsKey(_resource) || AcquiredLocks.Value[_resource] == 0)
            {
                _connection = storage.CreateAndOpenConnection();

                try
                {
                    Acquire(_connection, _resource, timeout);
                }
                catch (Exception ex) when (ex.IsCatchableExceptionType())
                {
                    storage.ReleaseConnection(_connection);
                    throw;
                }

                if (!_storage.IsExistingConnection(_connection))
                {
                    _timer = new Timer(ExecuteKeepAliveQuery, null, KeepAliveInterval, KeepAliveInterval);
                }

                AcquiredLocks.Value[_resource] = 1;
            }
            else
            {
                AcquiredLocks.Value[_resource]++;
            }
        }

        [SuppressMessage("Performance", "CA1854:Prefer the \'IDictionary.TryGetValue(TKey, out TValue)\' method")]
        public void Dispose()
        {
            if (_completed) return;

            _completed = true;

            if (!AcquiredLocks.Value.ContainsKey(_resource)) return;

            AcquiredLocks.Value[_resource]--;

            if (AcquiredLocks.Value[_resource] != 0) return;

            lock (_lockObject)
            {
                // Timer callback may be invoked after the Dispose method call,
                // so we are using lock to avoid unsynchronized calls.

                try
                {
                    AcquiredLocks.Value.Remove(_resource);

                    _timer?.Dispose();

                    if (_connection.State == ConnectionState.Open)
                    {
                        // Session-scoped application locks are held only when connection
                        // is open. When connection is closed or broken, for example, when
                        // there was an error, application lock is already released by SQL
                        // Server itself, and we shouldn't do anything.
                        Release(_connection, _resource);
                    }
                }
                finally
                {
                    _storage.ReleaseConnection(_connection);
                    _connection = null;
                }
            }

            GC.SuppressFinalize(this);
        }

        private void ExecuteKeepAliveQuery(object obj)
        {
            lock (_lockObject)
            {
                try
                {
                    _connection?.Execute("SELECT 1;");
                }
                catch
                {
                    // Connection is broken. This means that distributed lock
                    // was released, and we can't guarantee the safety property
                    // for the code that is wrapped with this block. So it was
                    // a bad idea to have a separate connection for just
                    // distributed lock.
                    
                    // OBSOLETE. This class is not used anymore by the SqlServerConnection
                    // class. The problem above was solved there by establishing a
                    // dedicated connection, when there is at least one acquired lock.
                    // Since the acquisition, all the commands and transactions are routed
                    // through that connection to ensure all the locks are still active.
                }
            }
        }

        internal static void Acquire(DbConnection connection, string resource, TimeSpan timeout)
        {
            if (connection.State != ConnectionState.Open)
            {
                // When we are passing a closed connection to Dapper's Execute method,
                // it kindly opens it for us, but after command execution, it will be closed
                // automatically, and our just-acquired application lock will immediately
                // be released. This is not behavior we want to achieve, so let's throw an
                // exception instead.
                throw new InvalidOperationException("Connection must be open before acquiring a distributed lock.");
            }

            var started = Stopwatch.StartNew();

            // We can't pass our timeout directly to the sp_getapplock stored procedure, because
            // high values, such as minute or more, may cause SQL Server's thread pool starvation,
            // when the number of connections that try to acquire a lock is more than the number of 
            // available threads in SQL Server. In this case a deadlock will occur, when SQL Server 
            // tries to schedule some more work for a connection that acquired a lock, but all the 
            // available threads in a pool waiting for that lock to be released.
            //
            // So we are trying to acquire a lock multiple times instead, with timeout that's equal
            // to seconds, not minutes.
            var lockTimeout = (long) Math.Min(LockTimeout.TotalMilliseconds, timeout.TotalMilliseconds);

            do
            {
                using var command = connection
                    .Create("sp_getapplock", CommandType.StoredProcedure, timeout: (int)(lockTimeout / 1000) + 5)
                    .AddParameter("@Resource", resource, DbType.String, size: 255)
                    .AddParameter("@DbPrincipal", "public", DbType.String, size: 32)
                    .AddParameter("@LockMode", LockMode, DbType.String, size: 32)
                    .AddParameter("@LockOwner", LockOwner, DbType.String, size: 32)
                    .AddParameter("@LockTimeout", lockTimeout, DbType.Int32)
                    .AddReturnParameter("@Result", out var resultParameter, DbType.Int32);

                command.ExecuteNonQuery();

                var lockResult = (int)resultParameter.Value;

                if (lockResult >= 0)
                {
                    // The lock has been successfully obtained on the specified resource.
                    return;
                }

                if (lockResult == -999 /* Indicates a parameter validation or other call error. */)
                {
                    throw new SqlServerDistributedLockException(
                        $"Could not place a lock on the resource '{resource}': {(LockErrorMessages.TryGetValue(lockResult, out var message) ? message : $"Server returned the '{lockResult}' error.")}.");
                }
            } while (started.Elapsed < timeout);

            throw new DistributedLockTimeoutException(resource);
        }

        internal static void Release(DbConnection connection, string resource)
        {
            using (var command = CreateReleaseCommand(connection, resource, out var resultParameter))
            {
                command.ExecuteNonQuery();

                var releaseResult = (int)resultParameter.Value;

                if (releaseResult < 0)
                {
                    throw new SqlServerDistributedLockException(
                        $"Could not release a lock on the resource '{resource}': Server returned the '{releaseResult}' error.");
                }
            }
        }

        internal static DbCommand CreateReleaseCommand(
            DbConnection connection,
            string resource,
            out DbParameter resultParameter)
        {
            return connection.Create("sp_releaseapplock", CommandType.StoredProcedure)
                .AddParameter("@Resource", resource, DbType.String, size: 255)
                .AddParameter("@LockOwner", LockOwner, DbType.String, size: 32)
                .AddReturnParameter("@Result", out resultParameter, DbType.Int32);
        }
    }
}
