using System;
using Sharp.Migrations;

namespace Hangfire.SharpData.Migrations {
    public class _007_CreateListTable : ReversibleSchemaMigration {
        public override void Up() {
            Add.Table("List").WithColumns(
                Column.AutoIncrement("Id").AsPrimaryKey(),
                Column.String("Key").Size(100).NotNull(),
                Column.String("Value").Size(Int32.MaxValue),
                Column.Date("ExpireAt")
            );
        }
    }
}