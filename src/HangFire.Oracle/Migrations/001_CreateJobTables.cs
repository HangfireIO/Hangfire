using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sharp.Migrations;

namespace Hangfire.SharpData.Migrations {
    public class _001_CreateJobTable : ReversibleSchemaMigration {
        public override void Up() {
            Add.Table("Job").WithColumns(
                Column.AutoIncrement("Id").AsPrimaryKey(),
                Column.Int32("StateId"),
                Column.String("StateName").Size(20).Comment("To speed-up queries"),
                Column.String("InvocationData").Size(Int32.MaxValue).NotNull(),
                Column.String("Arguments").Size(Int32.MaxValue).NotNull(),
                Column.Date("CreatedAt").NotNull(),
                Column.Date("ExpireAt")
            );
            Add.IndexKey("IX_HangFire_Job_StateName").OnColumns("StateName").OfTable("Job");
        }
    }
}
