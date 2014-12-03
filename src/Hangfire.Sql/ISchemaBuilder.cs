using System.Data;

namespace Hangfire.Sql {
    public interface ISchemaBuilder {
        void BuildDatabase(IDbConnection connection);
    }
}