using System;
using Sharp.Migrations;

namespace Hangfire.SharpData.Migrations {
    public class _002_CreateStateTable : ReversibleSchemaMigration {
        public override void Up() {
            Add.Table("State").WithColumns(
                Column.AutoIncrement("Id").AsPrimaryKey(),
                Column.Int32("JobId").NotNull(),
                Column.String("Name").Size(20).NotNull(),
                Column.String("Reason").Size(100),
                Column.Date("CreatedAt").NotNull(),
                Column.String("Data").Size(Int32.MaxValue)
                );
            Add.ForeignKey("FK_HangFire_State_Job")
                .OnColumn("JobId")
                .OfTable("State")
                .ReferencingColumn("Id")
                .OfTable("Job")
                .OnDeleteCascade();
            Add.IndexKey("IX_HangFire_State_JobId")
                .OnColumns("JobId")
                .OfTable("State");
        }
    }
}