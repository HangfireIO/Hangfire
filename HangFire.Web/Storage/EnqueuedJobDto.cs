using System;
using HangFire.Common;

namespace HangFire.Web
{
    internal class EnqueuedJobDto
    {
        public JobMethod Method { get; set; }
        public DateTime? EnqueuedAt { get; set; }
        public bool InEnqueuedState { get; set; }
    }
}