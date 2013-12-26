using System;
using System.Collections.Generic;
using HangFire.Common;

namespace HangFire.Web
{
    internal class FailedJobDto
    {
        public JobMethod Method { get; set; }
        public string[] Arguments { get; set; }
        public Dictionary<string, string> OldFormatArgs { get; set; }
        public DateTime? FailedAt { get; set; }
        public string ExceptionType { get; set; }
        public string ExceptionMessage { get; set; }
        public string ExceptionDetails { get; set; }
        public bool InFailedState { get; set; }
    }
}