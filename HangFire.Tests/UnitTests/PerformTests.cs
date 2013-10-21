using System;
using HangFire.Client;
using HangFire.States;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace HangFire.Tests.UnitTests
{
    [TestClass]
    public class PerformTests
    {
        private Mock<IJobClient> _jobClientMock;

        [TestInitialize]
        public void SetUp()
        {
            _jobClientMock = new Mock<IJobClient> { CallBase = false };

            Perform.CreateJobClientCallback 
                = () => _jobClientMock.Object;
        }

        [TestMethod]
        public void AsyncMethod_WithNoArguments()
        {
            Perform.Async(typeof (TestJob));

            _jobClientMock.Verify(
                x => x.CreateJob(
                    ItIsNonEmptyGuid(),
                    ItIsTestType(),
                    ItIsEnqueuedToTheDefaultQueue(),
                    It.Is<object>(y => y == null)),
                Times.Once);

            _jobClientMock.Verify(x => x.Dispose(), Times.Once);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AsyncMethod_WithNoArguments_AndNullType_ThrowsAnException()
        {
            Perform.Async(null);
        }

        [TestMethod]
        public void AsyncMethod_WithArguments()
        {
            Perform.Async(typeof (TestJob), new { Greeting = "Hello" });

            _jobClientMock.Verify(
                x => x.CreateJob(
                    ItIsNonEmptyGuid(),
                    ItIsTestType(),
                    ItIsEnqueuedToTheDefaultQueue(),
                    It.IsNotNull<object>()),
                Times.Once);

            _jobClientMock.Verify(x => x.Dispose(), Times.Once);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AsyncMethod_WithArguments_AndNullType_ThrowsAnException()
        {
            Perform.Async(null, new { Greeting = "Hello" });
        }

        [TestMethod]
        public void GenericAsyncMethod_WithNoArguments()
        {
            Perform.Async<TestJob>();

            _jobClientMock.Verify(
                x => x.CreateJob(
                    ItIsNonEmptyGuid(),
                    ItIsTestType(),
                    ItIsEnqueuedToTheDefaultQueue(),
                    It.Is<object>(y => y == null)),
                Times.Once);

            _jobClientMock.Verify(x => x.Dispose(), Times.Once);
        }

        [TestMethod]
        public void GenericAsyncMethod_WithArguments()
        {
            Perform.Async<TestJob>(new { ArticleId = 3 });

            _jobClientMock.Verify(
                x => x.CreateJob(
                    ItIsNonEmptyGuid(),
                    ItIsTestType(),
                    ItIsEnqueuedToTheDefaultQueue(),
                    It.IsNotNull<object>()),
               Times.Once);

            _jobClientMock.Verify(x => x.Dispose(), Times.Once);
        }

        [TestMethod]
        public void InMethod_WithNoArguments()
        {
            Perform.In(TimeSpan.FromDays(1), typeof (TestJob));

            _jobClientMock.Verify(
                x => x.CreateJob(
                    ItIsNonEmptyGuid(),
                    ItIsTestType(),
                    ItIsScheduledTomorrow(),
                    It.Is<object>(y => y == null)),
                Times.Once);

            _jobClientMock.Verify(x => x.Dispose(), Times.Once);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void InMethod_WithNoArguments_AndNullType_ThrowsAnException()
        {
            Perform.In(TimeSpan.FromDays(1), null);
        }

        [TestMethod]
        public void InMethod_WithArguments()
        {
            Perform.In(TimeSpan.FromDays(1), typeof (TestJob), new { Count = long.MaxValue });

            _jobClientMock.Verify(
                x => x.CreateJob(
                    ItIsNonEmptyGuid(),
                    ItIsTestType(),
                    ItIsScheduledTomorrow(),
                    It.IsNotNull<object>()),
                Times.Once);

            _jobClientMock.Verify(x => x.Dispose(), Times.Once);
        }

        [TestMethod]
        public void InMethod_WithNegativeInterval()
        {
            Perform.In(TimeSpan.FromDays(-1), typeof(TestJob));

            _jobClientMock.Verify(
                x => x.CreateJob(
                    It.IsAny<string>(),
                    It.IsAny<Type>(),
                    ItIsScheduledYesterday(),
                    It.IsAny<object>()));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void InMethod_WithArguments_AndNullType_ThrowsAnException()
        {
            Perform.In(TimeSpan.FromDays(1), null, new { Count = 1 });
        }

        [TestMethod]
        public void GenericInMethod_WithNoArguments()
        {
            Perform.In<TestJob>(TimeSpan.FromDays(1));

            _jobClientMock.Verify(
                x => x.CreateJob(
                    ItIsNonEmptyGuid(),
                    ItIsTestType(),
                    ItIsScheduledTomorrow(),
                    It.Is<object>(y => y == null)),
                Times.Once);

            _jobClientMock.Verify(x => x.Dispose(), Times.Once);
        }

        [TestMethod]
        public void GenericInMethod_WithArguments()
        {
            Perform.In<TestJob>(TimeSpan.FromDays(1), new { Greeting = "Hello" });

            _jobClientMock.Verify(
                x => x.CreateJob(
                    ItIsNonEmptyGuid(),
                    ItIsTestType(),
                    ItIsScheduledTomorrow(),
                    It.IsNotNull<object>()),
                Times.Once);

            _jobClientMock.Verify(x => x.Dispose(), Times.Once);
        }

        public static string ItIsNonEmptyGuid()
        {
            Func<string, bool> validator = actual =>
                {
                    var guid = Guid.Parse(actual);
                    return guid != Guid.Empty;
                };

            return It.Is<string>(x => validator(x));
        }

        public static Type ItIsTestType()
        {
            return It.Is<Type>(y => y == typeof (TestJob));
        }

        public static JobState ItIsEnqueuedToTheDefaultQueue()
        {
            return It.Is<EnqueuedState>(y => y.Queue == "default");
        }

        public static JobState ItIsScheduledTomorrow()
        {
            return
                It.Is<ScheduledState>(
                    x => DateTime.UtcNow.Date.AddDays(1) <= x.EnqueueAt && x.EnqueueAt < DateTime.UtcNow.AddDays(2));
        }

        public static JobState ItIsScheduledYesterday()
        {
            return
                It.Is<ScheduledState>(
                    x => DateTime.UtcNow.Date.AddDays(-2) < x.EnqueueAt && x.EnqueueAt <= DateTime.UtcNow.AddDays(-1));
        }
    }
}
