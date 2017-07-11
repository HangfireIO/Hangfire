using System;
using Xunit;

namespace Hangfire.SqlServer.Tests
{
    public class SqlServerMonitoringApiFacts
    {
        [Fact]
        public void Ctor_ThrowsAnException_WhenStorageIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new SqlServerMonitoringApi(null, null));

            Assert.Equal("storage", exception.ParamName);
        }
    }
}
