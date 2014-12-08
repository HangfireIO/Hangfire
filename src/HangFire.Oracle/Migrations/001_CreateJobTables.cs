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

using System;
using Sharp.Migrations;

namespace Hangfire.SharpData.Migrations
{
    public class _001_CreateJobTable : ReversibleSchemaMigration
    {
        public override void Up()
        {
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