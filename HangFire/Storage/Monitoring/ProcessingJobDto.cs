using System;
using HangFire.Common;

namespace HangFire.Storage.Monitoring
{
    public class ProcessingJobDto
    {
        public JobMethod Method { get; set; }
        public bool InProcessingState { get; set; }
        public string ServerName { get; set; }
        public DateTime? StartedAt { get; set; }
        public string State { get; set; }
    }
}