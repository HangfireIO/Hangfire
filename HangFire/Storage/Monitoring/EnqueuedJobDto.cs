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

        public JobMethod Method { get; set; }
        public DateTime? EnqueuedAt { get; set; }
        public bool InEnqueuedState { get; set; }
    }
}