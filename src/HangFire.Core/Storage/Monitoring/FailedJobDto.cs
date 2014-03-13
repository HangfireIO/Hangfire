using System;
using HangFire.Common;

namespace HangFire.Storage.Monitoring
{
    public class FailedJobDto
    {
        public FailedJobDto()
        {
            InFailedState = true;
        }

        public JobMethod Method { get; set; }
        public DateTime? FailedAt { get; set; }
        public string ExceptionType { get; set; }
        public string ExceptionMessage { get; set; }
        public string ExceptionDetails { get; set; }
        public bool InFailedState { get; set; }
    }
}