using System;

namespace HangFire.Web
{
    internal class ProcessingJobDto
    {
        public bool InProcessingState { get; set; }
        public string ServerName { get; set; }
        public string Queue { get; set; }
        public string Type { get; set; }
        public DateTime? StartedAt { get; set; }
        public string State { get; set; }
    }
}