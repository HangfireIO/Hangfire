using System;
using HangFire.Common;

namespace HangFire.Storage.Monitoring
{
    public class DequeuedJobDto
    {
        public JobMethod Method { get; set; }
        public string State { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? FetchedAt { get; set; }
        public DateTime? CheckedAt { get; set; }
    }
}
