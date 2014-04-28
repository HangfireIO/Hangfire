using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Dapper;
using HangFire.States;
using Moq;
using Xunit;

namespace HangFire.SqlServer.Tests
{
    public class WriteTransactionFacts
    {
        [Fact]
        public void Ctor_ThrowsAnException_IfConnectionIsNull()
        {
            Assert.Throws<ArgumentNullException>(
                () => new SqlServerWriteOnlyTransaction(null));
        }

        [Fact, CleanDatabase]
        public void ExpireJob_SetsJobExpirationData()
        {
            const string arrangeSql = @"
insert into HangFire.Job (InvocationData, Arguments, CreatedAt)
values ('', '', getutcdate())
select scope_identity() as Id";

            UseConnection(sql =>
            {
                var jobId = sql.Query(arrangeSql).Single().Id.ToString();
                var anotherJobId = sql.Query(arrangeSql).Single().Id.ToString();

                Commit(sql, x => x.ExpireJob(jobId, TimeSpan.FromDays(1)));

                var job = GetTestJob(sql, jobId);
                Assert.True(DateTime.UtcNow < job.ExpireAt && job.ExpireAt < DateTime.UtcNow.AddDays(1));

                var anotherJob = GetTestJob(sql, anotherJobId);
                Assert.Null(anotherJob.ExpireAt);
            });
        }

        [Fact, CleanDatabase]
        public void PersistJob_ClearsTheJobExpirationData()
        {
            const string arrangeSql = @"
insert into HangFire.Job (InvocationData, Arguments, CreatedAt, ExpireAt)
values ('', '', getutcdate(), getutcdate())
select scope_identity() as Id";

            UseConnection(sql =>
            {
                var jobId = sql.Query(arrangeSql).Single().Id.ToString();
                var anotherJobId = sql.Query(arrangeSql).Single().Id.ToString();

                Commit(sql, x => x.PersistJob(jobId));

                var job = GetTestJob(sql, jobId);
                Assert.Null(job.ExpireAt);

                var anotherJob = GetTestJob(sql, anotherJobId);
                Assert.NotNull(anotherJob.ExpireAt);
            });
        }

        [Fact, CleanDatabase]
        public void SetJobState_AppendsAStateAndSetItToTheJob()
        {
            const string arrangeSql = @"
insert into HangFire.Job (InvocationData, Arguments, CreatedAt)
values ('', '', getutcdate())
select scope_identity() as Id";

            UseConnection(sql =>
            {
                var jobId = sql.Query(arrangeSql).Single().Id.ToString();
                var anotherJobId = sql.Query(arrangeSql).Single().Id.ToString();

                var state = new Mock<State>();
                state.Setup(x => x.Name).Returns("State");
                state.Setup(x => x.SerializeData())
                    .Returns(new Dictionary<string, string> { { "Name", "Value" } });
                state.Object.Reason = "Reason";

                Commit(sql, x => x.SetJobState(jobId, state.Object));

                var job = GetTestJob(sql, jobId);
                Assert.Equal("State", job.StateName);
                Assert.NotNull(job.StateId);

                var anotherJob = GetTestJob(sql, anotherJobId);
                Assert.Null(anotherJob.StateName);
                Assert.Null(anotherJob.StateId);

                var jobState = sql.Query("select * from HangFire.State").Single();
                Assert.Equal((string)jobId, jobState.JobId.ToString());
                Assert.Equal("State", jobState.Name);
                Assert.Equal("Reason", jobState.Reason);
                Assert.NotNull(jobState.CreatedAt);
                Assert.Equal("{\"Name\":\"Value\"}", jobState.Data);
            });
        }

        [Fact, CleanDatabase]
        public void AddJobState_JustAddsANewRecordInATable()
        {
            const string arrangeSql = @"
insert into HangFire.Job (InvocationData, Arguments, CreatedAt)
values ('', '', getutcdate())
select scope_identity() as Id";

            UseConnection(sql =>
            {
                var jobId = sql.Query(arrangeSql).Single().Id.ToString();

                var state = new Mock<State>();
                state.Setup(x => x.Name).Returns("State");
                state.Setup(x => x.SerializeData())
                    .Returns(new Dictionary<string, string> { { "Name", "Value" } });
                state.Object.Reason = "Reason";

                Commit(sql, x => x.AddJobState(jobId, state.Object));

                var job = GetTestJob(sql, jobId);
                Assert.Null(job.StateName);
                Assert.Null(job.StateId);

                var jobState = sql.Query("select * from HangFire.State").Single();
                Assert.Equal((string)jobId, jobState.JobId.ToString());
                Assert.Equal("State", jobState.Name);
                Assert.Equal("Reason", jobState.Reason);
                Assert.NotNull(jobState.CreatedAt);
                Assert.Equal("{\"Name\":\"Value\"}", jobState.Data);
            });
        }

        [Fact, CleanDatabase]
        public void AddToQueue_AddsAJobToTheQueue()
        {
            UseConnection(sql =>
            {
                Commit(sql, x => x.AddToQueue("default", "1"));

                var record = sql.Query("select * from HangFire.JobQueue").Single();
                Assert.Equal("1", record.JobId.ToString());
                Assert.Equal("default", record.Queue);
                Assert.Null(record.FetchedAt);
            });
        }

        private static dynamic GetTestJob(IDbConnection connection, string jobId)
        {
            return connection
                .Query("select * from HangFire.Job where id = @id", new { id = jobId })
                .Single();
        }

        private void UseConnection(Action<SqlConnection> action)
        {
            using (var connection = ConnectionUtils.CreateConnection())
            {
                action(connection);
            }
        }

        private void Commit(
            SqlConnection connection,
            Action<SqlServerWriteOnlyTransaction> action)
        {
            using (var transaction = new SqlServerWriteOnlyTransaction(connection))
            {
                action(transaction);
                transaction.Commit();
            }
        }
    }
}
