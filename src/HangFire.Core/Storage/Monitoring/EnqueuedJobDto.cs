using System;
using HangFire.Common;

namespace HangFire.Storage.Monitoring
{
    public class EnqueuedJobDto
    {
        public EnqueuedJobDto()
        {
            InEnqueuedState = true;
        }

        public MethodData MethodData { get; set; }
        public DateTime? EnqueuedAt { get; set; }
        public bool InEnqueuedState { get; set; }
    }
}