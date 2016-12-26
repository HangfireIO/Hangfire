using System;
using System.Collections.Generic;
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
        private readonly PersistentJobQueueProviderCollection _queueProviders;
        private readonly Mock<IPersistentJobQueueMonitoringApi> _sqlServerJobQueueMonitoringApi;
        private const string InsertDummyJobSql = @"
insert into HangFire.[Job] ( [StateId], [StateName], [InvocationData],[Arguments],[CreatedAt],[ExpireAt])
values ( 1,@StateName,@Invocation,'', @CreatedAt, null);
select scope_identity() as Id";

        private const string InsertDummyStateForJobSql = @"
insert into HangFire.[State] ([JobId], [Name], [Reason], [CreatedAt],[Data])
values (@id, @name,@Reason, @CreatedAt, @Data) ;
Update HangFire.[Job] SET StateId = (select scope_identity() as Id) Where Id = @id
";
        public SqlServerMonitoringApiFacts()
        {
            var defaultProvider = new Mock<IPersistentJobQueueProvider>();
            defaultProvider.Setup(x => x.GetJobQueue())
                .Returns(new Mock<IPersistentJobQueue>().Object);
            _sqlServerJobQueueMonitoringApi = new Mock<IPersistentJobQueueMonitoringApi>();
            _sqlServerJobQueueMonitoringApi.Setup(t => t.GetQueues()).Returns(new List<string>());
            defaultProvider.Setup(t => t.GetJobQueueMonitoringApi()).Returns(_sqlServerJobQueueMonitoringApi.Object);
            _queueProviders = new PersistentJobQueueProviderCollection(defaultProvider.Object);
            _storage = new Mock<SqlServerStorage>(ConnectionUtils.GetConnectionString());
            _storage.Setup(t => t.QueueProviders).Returns(_queueProviders);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStorageIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new SqlServerMonitoringApi(null, null));

            Assert.Equal("storage", exception.ParamName);
        }



        [Fact,CleanDatabase]
        public void ProcessingJobs_ShouldReturnAllTheProcessedJobs()
        {

            var invocationString = JobHelper.ToJson(new InvocationData(null, null, null, null));
            var dictionKeys = new Dictionary<string, string> { { "StartedAt", "2016-12-10T18:54:08.5562518Z" }, { "ServerId","1" } };
            // PreRequisite 
            UseConnections((sql, connection) =>
            {
                var jobId =
                    sql.Query(InsertDummyJobSql,
                            new
                            {
                                CreatedAt = (DateTime?)DateTime.UtcNow.AddMinutes(60),
                                Invocation = invocationString,
                                StateName= "Processing"
                            })
                        .Single()
                        .Id.ToString();
                sql.Query(InsertDummyStateForJobSql, new
                {
                    id = jobId,
                    Name = "Processing",
                    Reason = "Processing",
                    CreatedAt = (DateTime?)DateTime.UtcNow.AddMinutes(60),
                    Data = JobHelper.ToJson(dictionKeys)
                });

                // Act
                var api = new SqlServerMonitoringApi(_storage.Object, null);
                var model = api.ProcessingJobs(0, 10);
                var modelCount = api.ProcessingCount();
                // Assert
                Assert.Equal(model.Count, 1);
                Assert.Equal(modelCount, 1);
            
            });
        }



        [Fact, CleanDatabase]
        public void DeletedJobs_ShouldReturnAllTheDeletedJobs()
        {

            var invocationString = JobHelper.ToJson(new InvocationData(null, null, null, null));
            var dictionKeys = new Dictionary<string, string> { { "DeletedAt", "2016-12-10T18:54:08.5562518Z" }, { "ServerId", "1" } };
            // PreRequisite 
            UseConnections((sql, connection) =>
            {
                var jobId =
                    sql.Query(InsertDummyJobSql,
                            new
                            {
                                CreatedAt = (DateTime?)DateTime.UtcNow.AddMinutes(60),
                                Invocation = invocationString,
                                StateName = "Deleted"
                            })
                        .Single()
                        .Id.ToString();
                sql.Query(InsertDummyStateForJobSql, new
                {
                    id = jobId,
                    Name = "Deleted",
                    Reason = "Deleted",
                    CreatedAt = (DateTime?)DateTime.UtcNow.AddMinutes(60),
                    Data = JobHelper.ToJson(dictionKeys)
                });
                // Act
                var api = new SqlServerMonitoringApi(_storage.Object, null);
                var model = api.DeletedJobs(0, 10);
                var modelCount = api.DeletedListCount();
                // Assert
                Assert.Equal(model.Count, 1);
                Assert.Equal(modelCount, 1);
              
              
            });
        }




        [Fact, CleanDatabase]
        public void FailedJobs_ShouldReturnAllTheFailedJobs()
        {

            var invocationString = JobHelper.ToJson(new InvocationData(null, null, null, null));
            var dictionKeys = new Dictionary<string, string> { { "FailedAt", "2016-12-10T18:54:08.5562518Z" }, { "ExceptionDetails", "D" } , { "ExceptionMessage", "M" } , { "ExceptionType", "T" } };
            // PreRequisite 
            UseConnections((sql, connection) =>
            {
                var jobId =
                    sql.Query(InsertDummyJobSql,
                            new
                            {
                                CreatedAt = (DateTime?)DateTime.UtcNow.AddMinutes(60),
                                Invocation = invocationString,
                                StateName= "Failed"
                            })
                        .Single()
                        .Id.ToString();
                sql.Query(InsertDummyStateForJobSql, new
                {
                    id = jobId,
                    Name = "Failed",
                    Reason = "Failed",
                    CreatedAt = (DateTime?)DateTime.UtcNow.AddMinutes(60),
                    Data = JobHelper.ToJson(dictionKeys)
                });

                // Act
                var api = new SqlServerMonitoringApi(_storage.Object, null);
                var model = api.FailedJobs(0, 10);
                var modelCount = api.FailedCount();
                // Assert
                Assert.Equal(model.Count, 1);
                Assert.Equal(modelCount, 1);


            });
        }


        [Fact,CleanDatabase]

        public void SkippedJobs_ShouldReturnAllTheSkippedJobs()
        {

            var invocationString = JobHelper.ToJson(new InvocationData(null, null, null, null));
            var dictionKeys = new Dictionary<string, string> { { "SkippedAt", "2016-12-10T18:54:08.5562518Z" } };
            // PreRequisite 
            UseConnections((sql, connection) =>
            {
                var jobId =
                    sql.Query(InsertDummyJobSql,
                            new
                            {
                                CreatedAt = (DateTime?) DateTime.UtcNow.AddMinutes(60),
                                Invocation = invocationString,
                                StateName = "Skipped"
                            })
                        .Single()
                        .Id.ToString();
                sql.Query(InsertDummyStateForJobSql, new
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
                var skippedCount = api.SkippedListCount();
                // Assert
                Assert.Equal(skippedModel.Count, 1);
                Assert.Equal(skippedCount, 1);
            });
        }



        [Fact, CleanDatabase]

        public void GetStatistics_ShouldRetriveAllTheStatus_IncludesSkippedJobs()
        {

            var invocationString = JobHelper.ToJson(new InvocationData(null, null, null, null));
            var dictionKeys = new Dictionary<string, string> { { "SkippedAt", "2016-12-10T18:54:08.5562518Z" } };
            // PreRequisite 
            UseConnections((sql, connection) =>
            {
                var jobId =
                    sql.Query(InsertDummyJobSql,
                            new
                            {
                                CreatedAt = (DateTime?)DateTime.UtcNow.AddMinutes(60),
                                Invocation = invocationString,
                                StateName = "Skipped"
                            })
                        .Single()
                        .Id.ToString();
                sql.Query(InsertDummyStateForJobSql, new
                {
                    id = jobId,
                    Name = "Skipped",
                    Reason = "Skipped",
                    CreatedAt = (DateTime?)DateTime.UtcNow.AddMinutes(60),
                    Data = JobHelper.ToJson(dictionKeys)
                });

                // Act
                var api = new SqlServerMonitoringApi(_storage.Object, null);
                var statisticsModel = api.GetStatistics();

                //Validate
                Assert.Equal(statisticsModel.Skipped, 1);
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
