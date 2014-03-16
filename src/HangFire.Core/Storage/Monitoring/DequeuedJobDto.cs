using System;
using HangFire.Common;

namespace HangFire.Storage.Monitoring
{
    public class DequeuedJobDto
    {
        public MethodData MethodData { get; set; }
        public string State { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? FetchedAt { get; set; }
    }
}
