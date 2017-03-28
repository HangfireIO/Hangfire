using Hangfire.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hangfire.Storage.Monitoring
{
 public  class SkippedJobDto
    {
        public SkippedJobDto()
        {
            IsSkipped = true;
        }
        public bool IsSkipped { get; set; }
        public Job Job { get; set; }
        public string Reason { get; set; }
        public DateTime? SkippedAt { get; set; }
    }
}
