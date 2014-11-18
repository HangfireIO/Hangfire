using System;
using Sharp.Migrations;

namespace Hangfire.SharpData.Migrations {
    public class _006_CreateHashTable : ReversibleSchemaMigration {
        public override void Up() {
            Add.Table("Hash").WithColumns(
                Column.AutoIncrement("Id").AsPrimaryKey(),
                Column.String("Key").Size(100).NotNull(),
                Column.String("Field").Size(100).NotNull(),
                Column.String("Value").Size(Int32.MaxValue),
                Column.Date("ExpireAt")
                );
            Add.UniqueKey("UX_HF_Hash_Key_Field")
                .OnColumns("Key", "Field")
                .OfTable("Hash");
        }
    }
}