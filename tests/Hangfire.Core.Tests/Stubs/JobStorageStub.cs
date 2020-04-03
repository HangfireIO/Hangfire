using System;
using Hangfire.Storage;

namespace Hangfire.Core.Tests.Stubs
{
    class JobStorageStub : JobStorage
    {
        public override IMonitoringApi GetMonitoringApi()
        {
            throw new NotImplementedException();
        }

        public override IStorageConnection GetConnection()
        {
            throw new NotImplementedException();
        }
    }
    class ClockStub : IClock
    {
        public DateTime UtcNow => throw new NotImplementedException();
    }
}
