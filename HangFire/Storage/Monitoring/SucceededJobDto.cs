using System;
using HangFire.Common;

namespace HangFire.Storage.Monitoring
{
    public class SucceededJobDto
    {
        public SucceededJobDto()
        {
            InSucceededState = true;
        }

        public JobMethod Method { get; set; }
        public DateTime? SucceededAt { get; set; }
        public bool InSucceededState { get; set; }
    }
}