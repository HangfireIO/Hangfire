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
using System.Data.SqlClient;
using Dapper;

namespace Hangfire.SqlServer
{
    internal class SqlServerDistributedLock : IDisposable
    {
        private const string LockMode = "Exclusive";
        private const string LockOwner = "Session";

        private static readonly IDictionary<int, string> LockErrorMessages
            = new Dictionary<int, string>
            {
                { -1, "The lock request timed out" },
                { -2, "The lock request was canceled" },
                { -3, "The lock request was chosen as a deadlock victim" },
                { -999, "Indicates a parameter validation or other call error" }
            };

        private readonly IDbConnection _connection;
        private readonly string _resource;

        private bool _completed;

        public SqlServerDistributedLock(string resource, TimeSpan timeout, IDbConnection connection)
        {
            if (String.IsNullOrEmpty(resource)) throw new ArgumentNullException("resource");
            if (connection == null) throw new ArgumentNullException("connection");

            _resource = resource;
            _connection = connection;

            var parameters = new DynamicParameters();
            parameters.Add("@Resource", _resource);
            parameters.Add("@DbPrincipal", "public");
            parameters.Add("@LockMode", LockMode);
            parameters.Add("@LockOwner", LockOwner);
            parameters.Add("@LockTimeout", timeout.TotalMilliseconds);
            parameters.Add("@Result", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);

            try
            {
                connection.Execute(
                @"sp_getapplock",
                parameters,
                null,
                timeout.Seconds,
                CommandType.StoredProcedure);

                var lockResult = parameters.Get<int>("@Result");

                if (lockResult < 0)
                {
                    throw CreateLockException(lockResult);
                }
            }
            catch (SqlException ex)
            {
                throw CreateLockException(ex.Number);
            }

        }

        private SqlServerDistributedLockException CreateLockException(int lockResult)
        {
            return new SqlServerDistributedLockException(
                String.Format(
                    "Could not place a lock on the resource '{0}': {1}.",
                    _resource,
                    LockErrorMessages.ContainsKey(lockResult)
                        ? LockErrorMessages[lockResult]
                        : String.Format("Server returned the '{0}' error.", lockResult)));
        }

        public void Dispose()
        {
            if (_completed) return;

            _completed = true;

            var parameters = new DynamicParameters();
            parameters.Add("@Resource", _resource);
            parameters.Add("@LockOwner", LockOwner);
            parameters.Add("@Result", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);

            _connection.Execute(
                @"sp_releaseapplock",
                parameters,
                commandType: CommandType.StoredProcedure);

            var releaseResult = parameters.Get<int>("@Result");

            if (releaseResult < 0)
            {
                throw new SqlServerDistributedLockException(
                    String.Format(
                        "Could not release a lock on the resource '{0}': Server returned the '{1}' error.",
                        _resource,
                        releaseResult));
            }
        }
    }
}