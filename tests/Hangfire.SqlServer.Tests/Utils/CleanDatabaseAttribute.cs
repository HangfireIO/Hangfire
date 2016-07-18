using System;
using System.Data.SqlClient;
using System.Reflection;
using System.Threading;
using System.Transactions;
using Dapper;
using Xunit.Sdk;

namespace Hangfire.SqlServer.Tests
{
    public class CleanDatabaseAttribute : BeforeAfterTestAttribute
    {
        private static readonly object GlobalLock = new object();
        private static bool _sqlObjectInstalled;
        
        private readonly IsolationLevel _isolationLevel;

        private TransactionScope _transaction;
        
        public CleanDatabaseAttribute(
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            _isolationLevel = isolationLevel;
        }

        public override void Before(MethodInfo methodUnderTest)
        {
            Monitor.Enter(GlobalLock);

            if (!_sqlObjectInstalled)
            {
                CreateAndInitializeDatabaseIfNotExists();
                _sqlObjectInstalled = true;
            }

            if (_isolationLevel != IsolationLevel.Unspecified)
            {
                _transaction = new TransactionScope(
                    TransactionScopeOption.RequiresNew,
                    new TransactionOptions { IsolationLevel = _isolationLevel });
            }
        }

        public override void After(MethodInfo methodUnderTest)
        {
            try
            {
                _transaction?.Dispose();
            }
            finally
            {
                Monitor.Exit(GlobalLock);
            }
            
        }

        private static void CreateAndInitializeDatabaseIfNotExists()
        {
            var recreateDatabaseSql = String.Format(
                @"if db_id('{0}') is null create database [{0}] COLLATE SQL_Latin1_General_CP1_CS_AS",
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
