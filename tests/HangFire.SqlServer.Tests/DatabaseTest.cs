using Xunit;

namespace HangFire.SqlServer.Tests
{
    public class DatabaseTest
    {
        [Fact, CleanDatabase]
        public void One_EqualsTo_One()
        {
            Assert.Equal(1, 1);
        }
    }
}
