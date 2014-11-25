using System;
using Sharp.Migrations;

namespace Hangfire.SharpData.Migrations {
    public class _005_CreateServerTable : ReversibleSchemaMigration {
        public override void Up() {
            Add.Table("Server").WithColumns(
                Column.String("Id").Size(50).AsPrimaryKey(),
                Column.String("Data").Size(4000),
                Column.Date("LastHeartbeat").NotNull()
            );
        }
    }
}