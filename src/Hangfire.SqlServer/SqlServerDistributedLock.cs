// This file is part of Hangfire.
// Copyright © 2013-2014 Sergey Odinokov.
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
using System.Threading;
using Dapper;
using Hangfire.Annotations;
using Hangfire.Storage;

namespace Hangfire.SqlServer
{
    public class SqlServerDistributedLock : IDisposable
    {
        private const string LockMode = "Exclusive";
        private const string LockOwner = "Session";
        private const int CommandTimeoutAdditionSeconds = 1;

        private static readonly IDictionary<int, string> LockErrorMessages
            = new Dictionary<int, string>
            {
                { -1, "The lock request timed out" },
                { -2, "The lock request was canceled" },
                { -3, "The lock request was chosen as a deadlock victim" },
                { -999, "Indicates a parameter validation or other call error" }
            };

        private static readonly ThreadLocal<Dictionary<string, int>> AcquiredLocks
            = new ThreadLocal<Dictionary<string, int>>(() => new Dictionary<string, int>()); 

        private readonly IDbConnection _connection;
        private readonly SqlServerStorage _storage;
        private readonly string _resource;

        private bool _completed;

        public SqlServerDistributedLock([NotNull] SqlServerStorage storage, [NotNull] string resource, TimeSpan timeout)
        {
            if (storage == null) throw new ArgumentNullException("storage");
            if (String.IsNullOrEmpty(resource)) throw new ArgumentNullException("resource");
            if ((timeout.TotalSeconds + CommandTimeoutAdditionSeconds) > Int32.MaxValue) throw new ArgumentException(string.Format("The timeout specified is too large. Please supply a timeout equal to or less than {0} seconds", Int32.MaxValue - CommandTimeoutAdditionSeconds), "timeout");

            _storage = storage;
            _resource = resource;
            _connection = storage.CreateAndOpenConnection();

            if (!AcquiredLocks.Value.ContainsKey(_resource))
            {
                Acquire(_connection, _resource, timeout);
                AcquiredLocks.Value[_resource] = 1;
            }
            else
            {
                AcquiredLocks.Value[_resource]++;
            }
        }

        public void Dispose()
        {
            if (_completed) return;

            _completed = true;

            if (!AcquiredLocks.Value.ContainsKey(_resource)) return;

            AcquiredLocks.Value[_resource]--;

            if (AcquiredLocks.Value[_resource] != 0) return;

            try
            {
                Release(_connection, _resource);
                AcquiredLocks.Value.Remove(_resource);
            }
            finally
            {
                _storage.ReleaseConnection(_connection);
            }
        }

        internal static void Acquire(IDbConnection connection, string resource, TimeSpan timeout)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@Resource", resource);
            parameters.Add("@DbPrincipal", "public");
            parameters.Add("@LockMode", LockMode);
            parameters.Add("@LockOwner", LockOwner);
            parameters.Add("@LockTimeout", timeout.TotalMilliseconds);
            parameters.Add("@Result", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);

            // Ensuring the timeout for the command is longer than the timeout specified for the stored procedure.
            var commandTimeout = (int)(timeout.TotalSeconds + CommandTimeoutAdditionSeconds);

            connection.Execute(
                @"sp_getapplock",
                parameters,
                commandTimeout: commandTimeout,
                commandType: CommandType.StoredProcedure);

            var lockResult = parameters.Get<int>("@Result");

            if (lockResult < 0)
            {
                if (lockResult == -1)
                {
                    throw new DistributedLockTimeoutException(resource);
                }

                throw new SqlServerDistributedLockException(
                    String.Format(
                    "Could not place a lock on the resource '{0}': {1}.",
                    resource,
                    LockErrorMessages.ContainsKey(lockResult)
                        ? LockErrorMessages[lockResult]
                        : String.Format("Server returned the '{0}' error.", lockResult)));
            }
        }

        internal static void Release(IDbConnection connection, string resource)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@Resource", resource);
            parameters.Add("@LockOwner", LockOwner);
            parameters.Add("@Result", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);

            connection.Execute(
                @"sp_releaseapplock",
                parameters,
                commandType: CommandType.StoredProcedure);

            var releaseResult = parameters.Get<int>("@Result");

            if (releaseResult < 0)
            {
                throw new SqlServerDistributedLockException(
                    String.Format(
                        "Could not release a lock on the resource '{0}': Server returned the '{1}' error.",
                        resource,
                        releaseResult));
            }
        }
    }
}