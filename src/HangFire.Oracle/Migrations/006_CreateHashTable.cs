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
    public class _006_CreateHashTable : ReversibleSchemaMigration
    {
        public override void Up()
        {
            Add.Table("HashTable").WithColumns(
                Column.AutoIncrement("Id").AsPrimaryKey(),
                Column.String("Key").Size(100).NotNull(),
                Column.String("Field").Size(100).NotNull(),
                Column.String("Value").Size(Int32.MaxValue),
                Column.Date("ExpireAt")
                );
            Add.UniqueKey("UX_HF_Hash_Key_Field")
                .OnColumns("Key", "Field")
                .OfTable("HashTable");
        }
    }
}