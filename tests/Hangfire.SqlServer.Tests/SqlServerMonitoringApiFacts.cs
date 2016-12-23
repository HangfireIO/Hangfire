using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Dapper;
using Hangfire.Common;
using Hangfire.Storage;
using Moq;
using Xunit;

// ReSharper disable AssignNullToNotNullAttribute

namespace Hangfire.SqlServer.Tests
{

    public class SqlServerMonitoringApiFacts
    {
        private readonly Mock<SqlServerStorage> _storage;

        public SqlServerMonitoringApiFacts()
        {
            _storage = new Mock<SqlServerStorage>(ConnectionUtils.GetConnectionString());
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStorageIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new SqlServerMonitoringApi(null, null));

            Assert.Equal("storage", exception.ParamName);
        }



        [Fact,CleanDatabase]

        public void SkippedJobsShouldReturnTheSkippedJobs()
        {

            var invocationString = JobHelper.ToJson(new InvocationData(null, null, null, null));
            var dictionKeys = new Dictionary<string, string> { { "SkippedAt", "2016-12-10T18:54:08.5562518Z" } };
            const string preRequisteJobSql = @"
insert into HangFire.[Job] ( [StateId], [StateName], [InvocationData],[Arguments],[CreatedAt],[ExpireAt])
values ( 1,'Skipped',@Invocation,'', @CreatedAt, null);
select scope_identity() as Id";

            const string preRequisteStateSql = @"
insert into HangFire.[State] ([JobId], [Name], [Reason], [CreatedAt],[Data])
values (@id, @name,@Reason, @CreatedAt, @Data) ;
Update HangFire.[Job] SET StateId = (select scope_identity() as Id) Where Id = @id
";

            // PreRequisite 
            UseConnections((sql, connection) =>
            {
                var jobId =
                    sql.Query(preRequisteJobSql,
                            new
                            {
                                CreatedAt = (DateTime?) DateTime.UtcNow.AddMinutes(60),
                                Invocation = invocationString
                            })
                        .Single()
                        .Id.ToString();
                sql.Query(preRequisteStateSql, new
                {
                    id = jobId,
                    Name = "Skipped",
                    Reason = "Skipped",
                    CreatedAt = (DateTime?) DateTime.UtcNow.AddMinutes(60),
                    Data = JobHelper.ToJson(dictionKeys)
                });

                // Act
                var api = new SqlServerMonitoringApi(_storage.Object, null);
                var skippedModel = api.SkippedJobs(0, 10);
                // Assert
                Assert.Equal(skippedModel.Count, 1);
            });
        }



        private static void UseConnections(Action<SqlConnection, SqlServerConnection> action)
        {
            using (var sqlConnection = ConnectionUtils.CreateConnection())
            {
                var storage = new SqlServerStorage(sqlConnection);
                using (var connection = new SqlServerConnection(storage))
                {
                    action(sqlConnection, connection);
                }
            }
        }
    }
}
