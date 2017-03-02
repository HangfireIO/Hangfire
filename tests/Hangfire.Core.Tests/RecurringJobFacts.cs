using Hangfire.Storage;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Hangfire.Core.Tests
{
    public class RecurringJobFacts
    {
        private readonly string _cronExpression;
        private readonly Func<string> _cronExpressionFunc;
        private readonly Expression<Action> _methodCall;
        private readonly Expression<Action<RecurringJobFacts>> _methodCallGeneric;
        private readonly Expression<Func<RecurringJobFacts, Task>> _methodCallGeneric2;
        private readonly Expression<Func<Task>> _methodCallGeneric3;
        private readonly TimeZoneInfo _timeZoneInfo;
        private readonly string _queue;
        private readonly Mock<JobStorage> _storage;
        private readonly Mock<IStorageConnection> _connection;
        private readonly Mock<IWriteOnlyTransaction> _transaction;
        private readonly string _jobId = "recurring-job-id";


        public RecurringJobFacts()
        {
            _cronExpression = Cron.Minutely();
            _cronExpressionFunc = () => CronExpressionMock();
            _methodCall = () => Method();
            _methodCallGeneric = (x) => MethodGeneric(this);
            _methodCallGeneric2 = (x) => MethodGeneric2(this);
            _methodCallGeneric3 = () => MethodGeneric3();
            _timeZoneInfo = TimeZoneInfo.Local;
            _queue = "default";
            _connection = new Mock<IStorageConnection>();
            _transaction = new Mock<IWriteOnlyTransaction>();
            _connection.Setup(x => x.CreateWriteTransaction()).Returns(_transaction.Object);
            _storage = new Mock<JobStorage>();
            _storage.Setup(x => x.GetConnection()).Returns(_connection.Object);

            GlobalConfiguration.Configuration.UseStorage<JobStorage>(_storage.Object);
        }

        [Fact]
        public void No_Exception_Verify_Overload1()
        {
            RecurringJob.AddOrUpdate(_methodCall, _cronExpressionFunc, _timeZoneInfo, _queue);
        }

        [Fact]
        public void No_Exception_Verify_Overload2()
        {
            RecurringJob.AddOrUpdate(_methodCall, _cronExpression, _timeZoneInfo, _queue);
        }

        [Fact]
        public void Throw_Exception_When_methodCall_is_null()
        {
            var exception = Assert.Throws<ArgumentNullException>(() =>
            {
                RecurringJob.AddOrUpdate(null, _cronExpressionFunc, _timeZoneInfo, _queue);
            });
            Assert.Equal("methodCall", exception.ParamName);
        }

        [Fact]
        public void Throw_Exception_When_methodCall_is_null_Overload1()
        {
            var exception = Assert.Throws<ArgumentNullException>(() =>
            {
                RecurringJob.AddOrUpdate(null, _cronExpression, _timeZoneInfo, _queue);
            });
            Assert.Equal("methodCall", exception.ParamName);
        }

        [Fact]
        public void Throw_Exception_When_methodCall_is_null_CronstringFormat()
        {
            var exception = Assert.Throws<ArgumentNullException>(() =>
            {
                RecurringJob.AddOrUpdate(null, _cronExpressionFunc, CronStringFormat.Default, _timeZoneInfo, _queue);
            });
            Assert.Equal("methodCall", exception.ParamName);
        }

        [Fact]
        public void Throw_Exception_When_methodCall_is_null_CronstringFormat_Overload1()
        {
            var exception = Assert.Throws<ArgumentNullException>(() =>
            {
                RecurringJob.AddOrUpdate(null, _cronExpression, CronStringFormat.Default, _timeZoneInfo, _queue);
            });
            Assert.Equal("methodCall", exception.ParamName);
        }

        [Fact]
        public void Throw_Exception_When_CronExpression_Is_Null()
        {
            var exception = Assert.Throws<NullReferenceException>(() =>
            {
                RecurringJob.AddOrUpdate(_methodCall, (Func<string>)null, _timeZoneInfo, _queue);
            });
        }

        [Fact]
        public void Throw_Exception_When_CronExpression_Is_Null_Overload1()
        {
            var exception = Assert.Throws<ArgumentNullException>(() =>
            {
                RecurringJob.AddOrUpdate(_methodCall, (string)null, _timeZoneInfo, _queue);
            });
            Assert.Equal("cronExpression", exception.ParamName);
        }

        [Fact]
        public void Throw_Exception_When_CronExpression_Is_Null_CronstringFormat()
        {
            var exception = Assert.Throws<NullReferenceException>(() =>
            {
                RecurringJob.AddOrUpdate(_methodCall, (Func<string>)null, CronStringFormat.Default, _timeZoneInfo, _queue);
            });
        }

        [Fact]
        public void Throw_Exception_When_CronExpression_Is_Null_CronstringFormat_Overload1()
        {
            var exception = Assert.Throws<ArgumentNullException>(() =>
            {
                RecurringJob.AddOrUpdate(_methodCall, (string)null, CronStringFormat.Default, _timeZoneInfo, _queue);
            });
            Assert.Equal("cronExpression", exception.ParamName);
        }

        [Fact]
        public void Throws_Exception_Invalid_Cronstring()
        {
            var exception = Assert.Throws<ArgumentException>(() =>
            {
                RecurringJob.AddOrUpdate(_methodCall, "bad cron expression", _timeZoneInfo, _queue);
            });
            Assert.Equal("cronExpression", exception.ParamName);
        }

        [Fact]
        public void Throws_Exception_Invalid_Cronstring_CronstringFormat()
        {
            var exception = Assert.Throws<ArgumentException>(() =>
            {
                RecurringJob.AddOrUpdate(_methodCall, "bad cron expression", CronStringFormat.Default, _timeZoneInfo, _queue);
            });
            Assert.Equal("cronExpression", exception.ParamName);
        }

        [Fact]
        public void No_Exception_JobId1()
        {
            RecurringJob.AddOrUpdate(_jobId, _methodCall, _cronExpression, _timeZoneInfo, _queue);
            RecurringJob.AddOrUpdate(_jobId, _methodCall, _cronExpression, CronStringFormat.Default, _timeZoneInfo, _queue);
        }

        [Fact]
        public void No_Exception_JobId2()
        {
            RecurringJob.AddOrUpdate<RecurringJobFacts>(_jobId, _methodCallGeneric, _cronExpression, _timeZoneInfo, _queue);
            RecurringJob.AddOrUpdate<RecurringJobFacts>(_jobId, _methodCallGeneric, _cronExpression, CronStringFormat.Default, _timeZoneInfo, _queue);
        }

        [Fact]
        public void No_Exception_Generic()
        {
            RecurringJob.AddOrUpdate<RecurringJobFacts>(_methodCallGeneric, _cronExpressionFunc, _timeZoneInfo, _queue);
            RecurringJob.AddOrUpdate<RecurringJobFacts>(_methodCallGeneric, _cronExpressionFunc, CronStringFormat.Default, _timeZoneInfo, _queue);
        }

        [Fact]
        public void No_Exception_Generic2()
        {
            RecurringJob.AddOrUpdate<RecurringJobFacts>(_methodCallGeneric2, _cronExpressionFunc, _timeZoneInfo, _queue);
            RecurringJob.AddOrUpdate<RecurringJobFacts>(_methodCallGeneric2, _cronExpressionFunc, CronStringFormat.Default, _timeZoneInfo, _queue);
        }

        [Fact]
        public void No_Exception_Generic3()
        {
            RecurringJob.AddOrUpdate<RecurringJobFacts>(_methodCallGeneric2, _cronExpression, _timeZoneInfo, _queue);
            RecurringJob.AddOrUpdate<RecurringJobFacts>(_methodCallGeneric2, _cronExpression, CronStringFormat.Default, _timeZoneInfo, _queue);
        }

        [Fact]
        public void No_Exception_Generic4()
        {
            RecurringJob.AddOrUpdate<RecurringJobFacts>(_jobId, _methodCallGeneric2, _cronExpression, _timeZoneInfo, _queue);
            RecurringJob.AddOrUpdate<RecurringJobFacts>(_jobId, _methodCallGeneric2, _cronExpression, CronStringFormat.Default, _timeZoneInfo, _queue);
        }

        [Fact]
        public void No_Exception_Generic5()
        {
            RecurringJob.AddOrUpdate(_jobId, _methodCallGeneric3, _cronExpression, _timeZoneInfo, _queue);
            RecurringJob.AddOrUpdate(_jobId, _methodCallGeneric3, _cronExpression, CronStringFormat.Default, _timeZoneInfo, _queue);
        }

        [Fact]
        public void No_Exception_Generic6()
        {
            RecurringJob.AddOrUpdate(_jobId, _methodCallGeneric2, _cronExpressionFunc, _timeZoneInfo, _queue);
            RecurringJob.AddOrUpdate(_jobId, _methodCallGeneric2, _cronExpressionFunc, CronStringFormat.Default, _timeZoneInfo, _queue);
        }

        [Fact]
        public void No_Exception_Generic7()
        {
            RecurringJob.AddOrUpdate(_jobId, _methodCallGeneric3, _cronExpressionFunc, _timeZoneInfo, _queue);
            RecurringJob.AddOrUpdate(_jobId, _methodCallGeneric3, _cronExpressionFunc, CronStringFormat.Default, _timeZoneInfo, _queue);
        }

        public void Method()
        {

        }

        public void MethodGeneric(RecurringJobFacts x)
        {
        }

        public Task MethodGeneric2(RecurringJobFacts x)
        {
            return Task.FromResult(x);
        }

        public Task MethodGeneric3()
        {
            return Task.FromResult("hello world");
        }

        public string CronExpressionMock()
        {
            return Cron.Minutely();
        }
    }
}
