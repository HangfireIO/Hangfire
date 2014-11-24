using System.Data;

namespace Hangfire.Sql {
    public interface IConnectionProvider {
        IDbConnection CreateConnection();
    }
}