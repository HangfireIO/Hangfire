using System;
using System.Data;
using Dapper;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.Sql;
using Hangfire.Sql.Entities;
using Oracle.ManagedDataAccess.Client;

namespace Hangfire.Oracle {
    public class OracleStorageConnection : SqlStorageConnection {
        public OracleStorageConnection(IDbConnection connection, SqlBook sqlBook, IDistributedLockAcquirer distributedLockAcquirer, PersistentJobQueueProviderCollection queueProviders) : base(connection, sqlBook, distributedLockAcquirer, queueProviders) {}

        public override void AnnounceServer(string serverId, ServerContext context) {
            base.AnnounceServer(serverId, context);
            //if (serverId == null) throw new ArgumentNullException("serverId");
            //if (context == null) throw new ArgumentNullException("context");

            //var data = new ServerData {
            //    WorkerCount = context.WorkerCount,
            //    Queues = context.Queues,
            //    StartedAt = DateTime.UtcNow,
            //};

            //var sql = String.Format(SqlBook.SqlConnection_AnnounceServer, serverId, )
            //Connection.Execute(SqlBook.SqlConnection_AnnounceServer);

            //var sqls = SqlBook.SqlConnection_AnnounceServer.Split(';');
            //try {
            //    Connection.Execute(sqls[0],
            //        new {id = serverId, data = JobHelper.ToJson(data), heartbeat = DateTime.UtcNow});
            //}
            //catch (OracleException) {
            //    Connection.Execute(sqls[1],
            //        new { id = serverId, data = JobHelper.ToJson(data), heartbeat = DateTime.UtcNow });                
            //}
            //Connection.Execute(SqlBook.SqlConnection_AnnounceServer,
            //    new { id = serverId, data = JobHelper.ToJson(data), heartbeat = DateTime.UtcNow });
        }
    }
}