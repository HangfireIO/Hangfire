using System;
using System.Collections.Generic;
using System.Linq;
using Hangfire.States;
using Moq;

namespace Hangfire.Core.Tests
{
    class ApplyStateContextMock
    {
        private readonly Lazy<ApplyStateContext> _context;

        public ApplyStateContextMock()
        {
            Storage = new Mock<JobStorage>();
            BackgroundJob = new BackgroundJobMock();
            NewStateValue = new Mock<IState>().Object;
            OldStateValue = null;
            TraversedStatesValue = Enumerable.Empty<IState>();

            _context = new Lazy<ApplyStateContext>(
                () => new ApplyStateContext(
                    Storage.Object,
                    BackgroundJob.Object,
                    NewStateValue,
                    OldStateValue,
                    TraversedStatesValue));
        }

        public Mock<JobStorage> Storage { get; set; }
        public BackgroundJobMock BackgroundJob { get; set; } 
        public IState NewStateValue { get; set; }
        public string OldStateValue { get; set; }
        public IEnumerable<IState> TraversedStatesValue { get; set; } 

        public ApplyStateContext Object
        {
            get { return _context.Value; }
        }
    }
}
