using System;
using Hangfire.States;
using Hangfire.Storage;
using Moq;

namespace Hangfire.Core.Tests
{
    class ElectStateContextMock
    {
        private readonly Lazy<ElectStateContext> _context;

        public ElectStateContextMock()
        {
            Storage = new Mock<JobStorage>();
            BackgroundJob = new BackgroundJobMock();
            ConnectionValue = new Mock<IStorageConnection>();
            CandidateStateValue = new Mock<IState>().Object;
            CurrentStateValue = "OldState";

            _context = new Lazy<ElectStateContext>(
                () => new ElectStateContext(
                    Storage.Object,
                    ConnectionValue.Object,
                    BackgroundJob.Object,
                    CandidateStateValue,
                    CurrentStateValue));
        }

        public Mock<JobStorage> Storage { get; set; } 
        public BackgroundJobMock BackgroundJob { get; set; }
        public IState CandidateStateValue { get; set; }
        public string CurrentStateValue { get; set; }
        public Mock<IStorageConnection> ConnectionValue { get; set; }

        public ElectStateContext Object
        {
            get { return _context.Value; }
        }
    }
}
