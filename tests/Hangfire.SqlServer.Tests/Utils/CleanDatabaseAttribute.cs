using System;
using System.Data.SqlClient;
using System.Reflection;
using System.Threading;
using System.Transactions;
using Dapper;
using Xunit;

namespace Hangfire.SqlServer.Tests
{
    public class CleanDatabaseAttribute : BeforeAfterTestAttribute
    {
        private static readonly object GlobalLock = new object();
        private static bool _sqlObjectInstalled;

        private readonly IsolationLevel _isolationLevel;
        private TransactionScope _transaction;
        private String[] _tablesToDelete = new string[0];

        public CleanDatabaseAttribute()
            : this(IsolationLevel.Serializable)
        {
        }

        public CleanDatabaseAttribute(IsolationLevel isolationLevel)
        {
            _isolationLevel = isolationLevel;
        }

        public CleanDatabaseAttribute(params string[] tablesToDelete) : this() {
            _tablesToDelete = tablesToDelete;
        }

        public override void Before(MethodInfo methodUnderTest)
        {
            Monitor.Enter(GlobalLock);

            if (!_sqlObjectInstalled)
            {
                RecreateDatabaseAndInstallObjects();
                _sqlObjectInstalled = true;
            }
            DeleteTables();
        }

        private void DeleteTables() {
            if (_tablesToDelete.Length == 0) {
                return;
            }
            using (var connection = ConnectionUtils.CreateConnectionProvider().CreateAndOpenConnection()) {
                using (var transaction = connection.BeginTransaction()) {
                    foreach (var table in _tablesToDelete) {
                        connection.Execute("delete from " + table, transaction: transaction);
                    }
                    transaction.Commit();
                }
            }
        }

        public override void After(MethodInfo methodUnderTest)
        {
            try
            {
                DeleteTables();
            }
            finally
            {
                Monitor.Exit(GlobalLock);
            }
            
        }

        private static void RecreateDatabaseAndInstallObjects()
        {
            var recreateDatabaseSql = String.Format(
                @"if db_id('{0}') is null create database [{0}]",
                ConnectionUtils.GetDatabaseName());

            using (var connection = new SqlConnection(
                ConnectionUtils.GetMasterConnectionString()))
            {
                connection.Execute(recreateDatabaseSql);
            }

            using (var connection = new SqlConnection(
                ConnectionUtils.GetConnectionString()))
            {
                new SqlServerObjectsInstaller().BuildDatabase(connection);
            }
        }
    }
}
