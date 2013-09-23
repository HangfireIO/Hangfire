using System;
using System.Collections.Generic;

namespace HangFire.Storage
{
    public class FailedJobDto
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string Queue { get; set; }
        public Dictionary<string, string> Args { get; set; }
        public DateTime? FailedAt { get; set; }
        public string ExceptionType { get; set; }
        public string ExceptionMessage { get; set; }
        public string ExceptionDetails { get; set; }
    }
}