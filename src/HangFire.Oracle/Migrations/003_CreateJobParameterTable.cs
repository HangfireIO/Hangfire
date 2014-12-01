using Sharp.Migrations;

namespace Hangfire.SharpData.Migrations {
    public class _003_CreateJobParameterTable : ReversibleSchemaMigration {
        public override void Up() {
            Add.Table("JobParameter").WithColumns(
                Column.AutoIncrement("Id").AsPrimaryKey(),
                Column.Int32("JobId").NotNull(),
                Column.String("Name").Size(40).NotNull(),
                Column.String("Value").Size(100)
                );
            Add.ForeignKey("FK_HF_JobParameter_Job")
                .OnColumn("JobId")
                .OfTable("JobParameter")
                .ReferencingColumn("Id")
                .OfTable("Job")
                .OnDeleteCascade();
            Add.IndexKey("IX_HF_JobPar_JobIdAndName")
                .OnColumns("JobId", "Name")
                .OfTable("JobParameter");
        }
    }
}