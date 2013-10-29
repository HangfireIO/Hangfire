using System;

namespace HangFire.Web
{
    internal class SucceededJobDto
    {
        public string Type { get; set; }
        public string Queue { get; set; }
        public DateTime? SucceededAt { get; set; }
        public bool InSucceededState { get; set; }
    }
}