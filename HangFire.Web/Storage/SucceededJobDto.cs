using System;
using HangFire.Common;

namespace HangFire.Web
{
    internal class SucceededJobDto
    {
        public JobMethod Method { get; set; }
        public DateTime? SucceededAt { get; set; }
        public bool InSucceededState { get; set; }
    }
}