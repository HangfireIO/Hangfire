using System;
using System.Collections.Generic;
using System.Threading;
using Hangfire.Profiling;
using Hangfire.States;
using Hangfire.Storage;
using Moq;

namespace Hangfire.Core.Tests
{
    class StateChangeContextMock
    {
        private readonly Lazy<StateChangeContext> _context;

        public StateChangeContextMock()
        {
            Storage = new Mock<JobStorage>();
            Connection = new Mock<IStorageConnection>();
            BackgroundJobId = "JobId";
            NewState = new Mock<IState>();
            ExpectedStates = null;
            DisableFilters = false;
            CancellationToken = CancellationToken.None;

            _context = new Lazy<StateChangeContext>(
                () => new StateChangeContext(
                    Storage.Object,
                    Connection.Object,
                    BackgroundJobId,
                    NewState.Object,
                    ExpectedStates,
                    DisableFilters,
                    CancellationToken,
                    EmptyProfiler.Instance));
        }

        public Mock<JobStorage> Storage { get; set; }
        public Mock<IStorageConnection> Connection { get; set; }
        public string BackgroundJobId { get; set; }
        public Mock<IState> NewState { get; set; }
        public IEnumerable<string> ExpectedStates { get; set; }
        public bool DisableFilters { get; set; }
        public CancellationToken CancellationToken { get; set; }
        
        public StateChangeContext Object => _context.Value;
    }
}
