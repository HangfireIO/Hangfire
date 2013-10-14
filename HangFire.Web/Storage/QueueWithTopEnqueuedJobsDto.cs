using System.Collections.Generic;

namespace HangFire.Web
{
    internal class QueueWithTopEnqueuedJobsDto
    {
        public string Name { get; set; }
        public long Length { get; set; }
        public IList<KeyValuePair<string, EnqueuedJobDto>> FirstJobs { get; set; }
    }
}
