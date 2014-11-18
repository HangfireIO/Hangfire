using System;
using Sharp.Migrations;

namespace Hangfire.SharpData.Migrations {
    public class _005_CreateServerTable : ReversibleSchemaMigration {
        public override void Up() {
            Add.Table("Server").WithColumns(
                Column.AutoIncrement("Id").AsPrimaryKey(),
                Column.String("Data").Size(Int32.MaxValue),
                Column.Date("LastHeartbeat").NotNull()
            );
        }
    }
}