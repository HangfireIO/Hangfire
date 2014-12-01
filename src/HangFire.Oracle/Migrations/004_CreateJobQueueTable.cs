using Sharp.Migrations;

namespace Hangfire.SharpData.Migrations {
    public class _004_CreateJobQueueTable : ReversibleSchemaMigration {
        public override void Up() {
            Add.Table("JobQueue").WithColumns(
                Column.AutoIncrement("Id").AsPrimaryKey(),
                Column.Int32("JobId").NotNull(),
                Column.String("Queue").Size(20).NotNull(),
                Column.Date("FetchedAt")
                );
            Add.ForeignKey("FK_HF_JobQueue_Job")
                .OnColumn("JobId")
                .OfTable("JobQueue")
                .ReferencingColumn("Id")
                .OfTable("Job")
                .OnDeleteCascade();
            Add.IndexKey("IX_HF_JQ_QueueAndFetchedAt")
                .OnColumns("Queue", "FetchedAt")
                .OfTable("JobQueue");
        }
    }
}