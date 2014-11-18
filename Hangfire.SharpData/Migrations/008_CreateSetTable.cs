using Sharp.Migrations;

namespace Hangfire.SharpData.Migrations {
    public class _008_CreateSetTable : ReversibleSchemaMigration {
        public override void Up() {
            Add.Table("Set").WithColumns(
                Column.AutoIncrement("Id").AsPrimaryKey(),
                Column.String("Key").Size(100).NotNull(),
                Column.Single("Score").NotNull(),
                Column.String("Value").Size(256),
                Column.Date("ExpireAt")
            );
            Add.UniqueKey("UX_HF_Set_KeyAndValue")
                .OnColumns("Key", "Value")
                .OfTable("Set");
        }
    }
}