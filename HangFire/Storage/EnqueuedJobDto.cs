using System;
using System.Collections.Generic;

namespace HangFire.Storage
{
    public class EnqueuedJobDto
    {
        public string Type { get; set; }
        public Dictionary<string, string> Args { get; set; }
        public DateTime EnqueuedAt { get; set; }
    }
}