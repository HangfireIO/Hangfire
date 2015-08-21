using System;
using Hangfire.Common;
using Hangfire.States;
using Moq;

namespace Hangfire.Core.Tests
{
    class StateContextMock
    {
        private readonly Lazy<StateContext> _context;

        public StateContextMock()
        {
            Storage = new Mock<JobStorage>();
            BackgroundJob = new BackgroundJobMock();

            _context = new Lazy<StateContext>(
                () => new StateContext(Storage.Object, BackgroundJob.Object));
        }

        public Mock<JobStorage> Storage { get; set; }
        public BackgroundJobMock BackgroundJob { get; set; }

        public StateContext Object
        {
            get { return _context.Value; }
        }
    }
}
