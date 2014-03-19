using System;
using System.Data.SqlClient;
using System.Reflection;
using System.Threading;
using System.Transactions;
using Dapper;
using Xunit;

namespace HangFire.SqlServer.Tests
{
    public class CleanDatabaseAttribute : BeforeAfterTestAttribute
    {
        private static readonly object GlobalLock = new object();
        private static bool _sqlObjectInstalled;

        private TransactionScope _transaction;

        public override void Before(MethodInfo methodUnderTest)
        {
            Monitor.Enter(GlobalLock);

            if (!_sqlObjectInstalled)
            {
                RecreateDatabaseAndInstallObjects();
                _sqlObjectInstalled = true;
            }

            _transaction = new TransactionScope(
                TransactionScopeOption.RequiresNew,
                new TransactionOptions { IsolationLevel = IsolationLevel.Serializable });
        }

        public override void After(MethodInfo methodUnderTest)
        {
            try
            {
                _transaction.Dispose();
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
                SqlServerObjectsInstaller.Install(connection);
            }
        }
    }
}
