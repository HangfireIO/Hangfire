using System;
using HangFire.Common;

namespace HangFire.Web
{
    internal class DequeuedJobDto
    {
        public JobMethod Method { get; set; }
        public string State { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? FetchedAt { get; set; }
        public DateTime? CheckedAt { get; set; }
    }
}
