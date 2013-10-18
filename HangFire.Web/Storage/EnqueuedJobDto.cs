using System;
using System.Collections.Generic;

namespace HangFire.Web
{
    internal class EnqueuedJobDto
    {
        public string Type { get; set; }
        public Dictionary<string, string> Args { get; set; }
        public DateTime? EnqueuedAt { get; set; }
        public bool InEnqueuedState { get; set; }
    }
}