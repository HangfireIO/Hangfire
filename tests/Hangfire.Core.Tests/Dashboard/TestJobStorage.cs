using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hangfire.Storage;

namespace Hangfire.Core.Tests.Dashboard
{
    class TestJobStorage : JobStorage
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
}
