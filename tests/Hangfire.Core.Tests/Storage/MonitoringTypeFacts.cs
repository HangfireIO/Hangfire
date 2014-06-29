using System.Collections.Generic;
using Hangfire.Storage.Monitoring;
using Xunit;

namespace Hangfire.Core.Tests.Storage
{
    public class MonitoringTypeFacts
    {
        [Fact]
        public void EnqueuedJobDto_Ctor_SetsInEnqueuedState()
        {
            Assert.True(new EnqueuedJobDto().InEnqueuedState);
        }

        [Fact]
        public void FailedJobDto_Ctor_SetsInFailedState()
        {
            Assert.True(new FailedJobDto().InFailedState);
        }

        [Fact]
        public void ProcessingJobDto_Ctor_SetsInProcessingState()
        {
            Assert.True(new ProcessingJobDto().InProcessingState);
        }

        [Fact]
        public void ScheduledJobDto_Ctor_SetsInScheduledState()
        {
            Assert.True(new ScheduledJobDto().InScheduledState);
        }

        [Fact]
        public void SucceededJobDto_Ctor_SetsInSucceededState()
        {
            Assert.True(new SucceededJobDto().InSucceededState);
        }

        [Fact]
        public void DeletedJobDto_Ctor_SetsInDeletedState()
        {
            Assert.True(new DeletedJobDto().InDeletedState);
        }

        [Fact]
        public void JobList_Ctor_ShouldInitializeCollection()
        {
            var list = new JobList<int>(new Dictionary<string, int> { { "1", 2 } });
            Assert.Equal(1, list.Count);
        }
    }
}
