using System;
using Hangfire.States;
using Xunit;

namespace Hangfire.Core.Tests
{
    public class LatencyTimeoutAttributeFacts
    {
        private const string JobId = "id";

        private readonly ElectStateContextMock _context;

        public LatencyTimeoutAttributeFacts()
        {
            var state = new ProcessingState("Default", "1");

            _context = new ElectStateContextMock();
            _context.ApplyContext.BackgroundJob.Id = JobId;
            _context.ApplyContext.NewStateObject = state;
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenTimeoutInSecondsValueIsNegative()
        {
            var exception = Assert.Throws<ArgumentOutOfRangeException>(
                () => CreateFilter(-1));
            
            Assert.Equal("timeoutInSeconds", exception.ParamName);
        }

        [Fact]
        public void OnStateElection_ChangesToDeleted_IfTimeoutExceeded()
        {
            _context.ApplyContext.BackgroundJob.CreatedAt = DateTime.UtcNow.AddMinutes(-1);

            var filter = CreateFilter(10);
            filter.OnStateElection(_context.Object);

            Assert.IsType<DeletedState>(_context.Object.CandidateState);
        }

        [Fact]
        public void OnStateElection_DoesNotChangeAnything_IfTimeoutNotExceeded()
        {
            _context.ApplyContext.BackgroundJob.CreatedAt = DateTime.UtcNow;

            var filter = CreateFilter(100);
            filter.OnStateElection(_context.Object);

            Assert.IsType<ProcessingState>(_context.Object.CandidateState);
        }

        private static LatencyTimeoutAttribute CreateFilter(int timeout)
        {
            return new LatencyTimeoutAttribute(timeout);
        }
    }
}
