using System;

namespace HangFire.Web
{
    internal class DequeuedJobDto
    {
        public string Type { get; set; }
        public string State { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? FetchedAt { get; set; }
        public DateTime? CheckedAt { get; set; }
    }
}
