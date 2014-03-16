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

        public MethodData MethodData { get; set; }
        public DateTime? SucceededAt { get; set; }
        public bool InSucceededState { get; set; }
    }
}