using System;
using System.Collections.Generic;

namespace HangFire.Web
{
    internal class ProcessingJobDto
    {
        public string ServerName { get; set; }
        public string Type { get; set; }
        public IDictionary<string, string> Args { get; set; }
        public DateTime StartedAt { get; set; }
    }
}