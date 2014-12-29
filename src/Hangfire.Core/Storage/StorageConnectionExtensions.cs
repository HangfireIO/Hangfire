﻿// This file is part of Hangfire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// Hangfire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// Hangfire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with Hangfire. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using Hangfire.Annotations;
using Hangfire.Common;

namespace Hangfire.Storage
{
    public static class StorageConnectionExtensions
    {
        public static List<RecurringJobDto> GetRecurringJobs([NotNull] this IStorageConnection connection)
        {
            if (connection == null) throw new ArgumentNullException("connection");

            var result = new List<RecurringJobDto>();

            var ids = connection.GetAllItemsFromSet("recurring-jobs");

            foreach (var id in ids)
            {
                var hash = connection.GetAllEntriesFromHash(String.Format("recurring-job:{0}", id));

                if (hash == null)
                {
                    result.Add(new RecurringJobDto { Id = id, Removed = true });
                    continue;
                }

                var dto = new RecurringJobDto { Id = id };
                dto.Cron = hash["Cron"];

                try
                {
                    var invocationData = JobHelper.FromJson<InvocationData>(hash["Job"]);
                    dto.Job = invocationData.Deserialize();
                }
                catch (JobLoadException ex)
                {
                    dto.LoadException = ex;
                }

                if (hash.ContainsKey("NextExecution"))
                {
                    dto.NextExecution = JobHelper.DeserializeDateTime(hash["NextExecution"]);
                }

                if (hash.ContainsKey("LastJobId") && !string.IsNullOrWhiteSpace(hash["LastJobId"]))
                {
                    dto.LastJobId = hash["LastJobId"];

                    var stateData = connection.GetStateData(dto.LastJobId);
                    if (stateData != null)
                    {
                        dto.LastJobState = stateData.Name;
                    }
                }

                if (hash.ContainsKey("LastExecution"))
                {
                    dto.LastExecution = JobHelper.DeserializeDateTime(hash["LastExecution"]);
                }

                result.Add(dto);
            }

            return result;
        }
    }
}
