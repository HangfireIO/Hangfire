using Hangfire.Dashboard;

namespace Hangfire.Core.Tests.Stubs
{
    class DashboardContextStub : DashboardContext
    {
        public DashboardContextStub(DashboardOptions options) : base(new JobStorageStub(), new ClockStub(), options)
        {
            Response = new DashboardResponseStub();
        }
    }
}
