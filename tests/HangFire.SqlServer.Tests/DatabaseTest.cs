using System.Data.SqlClient;
using System.Linq;
using Dapper;
using Xunit;

namespace HangFire.SqlServer.Tests
{
    public class DatabaseTest
    {
        [Fact, CleanDatabase]
        public void One_EqualsTo_One()
        {
            using (var connection = new SqlConnection(ConnectionUtils.GetConnectionString()))
            {
                var jobCount = connection.Query<int>(@"select count(*) from HangFire.Job").Single();
                Assert.Equal(0, jobCount);
            }
        }
    }
}
