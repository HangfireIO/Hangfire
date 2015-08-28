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
            NewState = new Mock<IState>();
            OldStateName = null;
            JobExpirationTimeout = TimeSpan.FromMinutes(1);

            _context = new Lazy<ApplyStateContext>(
                () => new ApplyStateContext(
                    Storage.Object,
                    Connection.Object,
                    Transaction.Object,
                    BackgroundJob.Object,
                    NewStateObject ?? NewState.Object,
                    OldStateName)
                {
                    JobExpirationTimeout = JobExpirationTimeout
                });
        }

        public Mock<JobStorage> Storage { get; set; }
        public Mock<IStorageConnection> Connection { get; set; } 
        public Mock<IWriteOnlyTransaction> Transaction { get; set; } 
        public BackgroundJobMock BackgroundJob { get; set; } 
        public IState NewStateObject { get; set; }
        public Mock<IState> NewState { get; set; }
        public string OldStateName { get; set; }
        public TimeSpan JobExpirationTimeout { get; set; } 

        public ApplyStateContext Object
        {
            get { return _context.Value; }
        }
    }
}
