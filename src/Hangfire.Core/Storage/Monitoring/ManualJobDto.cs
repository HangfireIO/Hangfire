using System;
using Hangfire.Common;

namespace Hangfire.Storage.Monitoring
{
    public class ManualJobDto
    {
        public Job Job { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsManualState { get; set; } = true;
    }
}
