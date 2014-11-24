using Sharp.Migrations;

namespace Hangfire.SharpData.Migrations {
    public class _009_CreateCounterTable : ReversibleSchemaMigration {
        public override void Up() {
            Add.Table("Counter").WithColumns(
                Column.AutoIncrement("Id").AsPrimaryKey(),
                Column.String("Key").Size(100).NotNull(),
                Column.Int16("Value").NotNull(),
                Column.Date("ExpireAt")
            );
            Add.IndexKey("IX_HF_Counter_Key")
                .OnColumns("Key")
                .OfTable("Counter");
        }
    }
}