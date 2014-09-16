using System;
using System.Runtime.InteropServices;
using System.Threading;
using Hangfire.States;
using Hangfire.Storage;
using Moq;
using NUnit.Framework;
using Xunit;
using Assert = Xunit.Assert;

namespace Hangfire.Core.Tests
{
    public class DeleteOnLatencyAttributeFacts
    {
        private const string JobId = "id";

        private readonly ProcessingState _state;
        private readonly Mock<IStorageConnection> _connection;
        private readonly ElectStateContextMock _context;

        public DeleteOnLatencyAttributeFacts()
        {
            _state = new ProcessingState("Default",1);
            _connection = new Mock<IStorageConnection>();

            _context = new ElectStateContextMock();
            _context.StateContextValue.JobIdValue = JobId;
            _context.StateContextValue.ConnectionValue = _connection;
            _context.CandidateStateValue = _state;
            _context.StateContextValue.CreatedAtValue = DateTime.UtcNow.AddSeconds(-1);

        }

        [Fact]
        public void Ctor_SetsDefaultLatencyTimeoutTo300SecodsByDefault()
        {
            var filter = new DeleteOnLatencyTimeoutAttribute();
            Assert.Equal(300,filter.TimeoutInSeconds);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenTimeoutInSecondsValueIsNegative()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => CreateFilter(-1));
        }

        [Fact]
        public void OnStateElection_DoesNotChangeAnything_IfTimeoutNotExceeded()
        {
            var filter = CreateFilter();
            filter.OnStateElection(_context.Object);

            Assert.IsType<ProcessingState>(_context.Object.CandidateState);
        }

        [Fact]
        public void OnStateElection_ChangesToDeleted_IfTimeoutExceeded()
        {
            var filter = CreateFilter(0);
            filter.OnStateElection(_context.Object);

            Assert.IsType<DeletedState>(_context.Object.CandidateState);
        }

        private static DeleteOnLatencyTimeoutAttribute CreateFilter(int delayInSeconds = 100)
        {
            return new DeleteOnLatencyTimeoutAttribute {TimeoutInSeconds = delayInSeconds};
        }
    }
}