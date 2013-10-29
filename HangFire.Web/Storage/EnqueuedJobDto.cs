using System;

namespace HangFire.Web
{
    internal class EnqueuedJobDto
    {
        public string Type { get; set; }
        public DateTime? EnqueuedAt { get; set; }
        public bool InEnqueuedState { get; set; }
    }
}