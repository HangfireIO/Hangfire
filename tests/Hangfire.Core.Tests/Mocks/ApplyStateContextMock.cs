using System;
using System.Collections.Generic;
using System.Linq;
using Hangfire.States;
using Hangfire.Storage;
using Moq;

namespace Hangfire.Core.Tests
{
    class ApplyStateContextMock
    {
        private readonly Lazy<ApplyStateContext> _context;

        public ApplyStateContextMock()
        {
            Storage = new Mock<JobStorage>();
            Connection = new Mock<IStorageConnection>();
            Transaction = new Mock<IWriteOnlyTransaction>();
            BackgroundJob = new BackgroundJobMock();
            NewStateValue = new Mock<IState>().Object;
            OldStateValue = null;
            TraversedStatesValue = Enumerable.Empty<IState>();

            _context = new Lazy<ApplyStateContext>(
                () => new ApplyStateContext(
                    Storage.Object,
                    Connection.Object,
                    Transaction.Object,
                    BackgroundJob.Object,
                    NewStateValue,
                    OldStateValue,
                    TraversedStatesValue));
        }

        public Mock<JobStorage> Storage { get; set; }
        public Mock<IStorageConnection> Connection { get; set; } 
        public Mock<IWriteOnlyTransaction> Transaction { get; set; } 
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
