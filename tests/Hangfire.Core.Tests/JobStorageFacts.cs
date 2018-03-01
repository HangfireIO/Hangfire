using System;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests
{
    public class JobStorageFacts
    {
        private readonly Mock<JobStorage> _storage;

        public JobStorageFacts()
        {
            _storage = new Mock<JobStorage>() { CallBase = true };
        }

        [Fact, GlobalLock(Reason = "Access static JobStorage.Current member")]
        public void SetCurrent_DoesNotThrowAnException_WhenValueIsNull()
        {
            // Does not throw
            JobStorage.Current = null;
        }

        [Fact, GlobalLock(Reason = "Access static JobStorage.Current member")]
        public void GetCurrent_ThrowsAnException_OnUninitializedValue()
        {
            JobStorage.Current = null;

            Assert.Throws<InvalidOperationException>(() => JobStorage.Current);
        }

        [Fact, GlobalLock(Reason = "Access static JobStorage.Current member")]
        public void GetCurrent_ReturnsCurrentValue_WhenInitialized()
        {
            var storage = new Mock<JobStorage>();
            JobStorage.Current = storage.Object;

            Assert.Same(storage.Object, JobStorage.Current);
        }

        [Fact]
        public void GetComponents_ReturnsEmptyCollectionByDefault()
        {
            Assert.Empty(_storage.Object.GetComponents());
        }

        [Fact]
        public void GetStateHandlers_ReturnsEmptyCollectionByDefault()
        {
            Assert.Empty(_storage.Object.GetStateHandlers());
        }

        [Fact]
        public void JobExpirationTimeout_HasDefaultTimeoutFromDays1()
        {
            Assert.Equal(TimeSpan.FromDays(1), _storage.Object.JobExpirationTimeout);
        }

        [Fact]
        public void JobExpirationTimeout_CantAllowTimeoutLessThanOneHour()
        {
            var oneMilisecondLess = TimeSpan.FromHours(1).Subtract(TimeSpan.FromMilliseconds(1));
            var exception = Assert.Throws<ArgumentException>(() => _storage.Object.JobExpirationTimeout = oneMilisecondLess);

            Assert.Equal("JobStorage.JobExpirationTimeout value should be equal or greater than 1 hour.", exception.Message);

        }

        [Fact]
        public void JobExpirationTimeout_CanChangeTheTimeout()
        {
            Assert.Equal(TimeSpan.FromDays(1), _storage.Object.JobExpirationTimeout);

            _storage.Object.JobExpirationTimeout = TimeSpan.FromHours(1);

            Assert.Equal(TimeSpan.FromHours(1), _storage.Object.JobExpirationTimeout);

            GlobalConfiguration.Configuration
                .UseStorage(_storage.Object)
                .WithJobExpirationTimeout(TimeSpan.FromDays(3));

            Assert.Equal(TimeSpan.FromDays(3), _storage.Object.JobExpirationTimeout);
        }
    }
}
