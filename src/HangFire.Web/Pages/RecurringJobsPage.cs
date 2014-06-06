using System;
using System.Collections.Generic;
using HangFire.Common;
using HangFire.Storage;

namespace HangFire.Web.Pages
{
    partial class RecurringJobsPage
    {
        public RecurringJobsPage()
        {
            RecurringJobs = new List<RecurringJobDto>();

            using (var connection = JobStorage.Current.GetConnection())
            {
                var ids = connection.GetAllItemsFromSet("recurring-jobs");

                foreach (var id in ids)
                {
                    var hash = connection.GetAllEntriesFromHash(String.Format("recurring-job:{0}", id));

                    if (hash == null)
                    {
                        RecurringJobs.Add(new RecurringJobDto { Id = id, Removed = true });
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
                        dto.NextExecution = JobHelper.FromStringTimestamp(hash["NextExecution"]);
                    }

                    if (hash.ContainsKey("LastJobId"))
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
                        dto.LastExecution = JobHelper.FromStringTimestamp(hash["LastExecution"]);
                    }

                    RecurringJobs.Add(dto);
                }
            }
        }

        public List<RecurringJobDto> RecurringJobs { get; private set; } 

        public class RecurringJobDto
        {
            public string Id { get; set; }
            public string Cron { get; set; }
            public Job Job { get; set; }
            public JobLoadException LoadException { get; set; }
            public DateTime? NextExecution { get; set; }
            public string LastJobId { get; set; }
            public string LastJobState { get; set; }
            public DateTime? LastExecution { get; set; }
            public bool Removed { get; set; }
        }
    }
}
