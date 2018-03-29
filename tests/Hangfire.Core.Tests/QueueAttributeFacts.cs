using Hangfire.States;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests
{
    public class QueueAttributeFacts
    {
        private readonly ElectStateContextMock _context;

        public QueueAttributeFacts()
        {
            _context = new ElectStateContextMock
            {
                ApplyContext = { NewStateObject = new EnqueuedState("queue") }
            };
        }

        [Fact]
        public void Ctor_CorrectlySets_AllPropertyValues()
        {
            var filter = new QueueAttribute("hello");
            Assert.Equal("hello", filter.Queue);
        }
    }
}
