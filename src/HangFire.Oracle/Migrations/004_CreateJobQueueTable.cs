// // This file is part of Hangfire.
// // Copyright © 2013-2014 Sergey Odinokov.
// // 
// // Hangfire is free software: you can redistribute it and/or modify
// // it under the terms of the GNU Lesser General Public License as 
// // published by the Free Software Foundation, either version 3 
// // of the License, or any later version.
// // 
// // Hangfire is distributed in the hope that it will be useful,
// // but WITHOUT ANY WARRANTY; without even the implied warranty of
// // MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// // GNU Lesser General Public License for more details.
// // 
// // You should have received a copy of the GNU Lesser General Public 
// // License along with Hangfire. If not, see <http://www.gnu.org/licenses/>.

using Sharp.Migrations;

namespace Hangfire.SharpData.Migrations
{
    public class _004_CreateJobQueueTable : ReversibleSchemaMigration
    {
        public override void Up()
        {
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