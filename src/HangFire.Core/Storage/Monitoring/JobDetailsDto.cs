using System;
using System.Collections.Generic;
using HangFire.Common;

namespace HangFire.Storage.Monitoring
{
    public class JobDetailsDto
    {
        public MethodData MethodData { get; set; }
        public string[] Arguments { get; set; }
        public IDictionary<string, string> OldFormatArguments { get; set; }
        public DateTime? CreatedAt { get; set; }
        public IDictionary<string, string> Properties { get; set; }
        public IList<StateHistoryDto> History { get; set; }
    }
}
