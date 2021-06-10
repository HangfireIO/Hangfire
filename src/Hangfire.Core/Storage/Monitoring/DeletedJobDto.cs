using System;
using System.Collections.Generic;
using Hangfire.Common;

namespace Hangfire.Storage.Monitoring
{
    public class DeletedJobDto
    {
        public DeletedJobDto()
        {
            InDeletedState = true;
        }

        public Job Job { get; set; }
        public JobLoadException LoadException { get; set; }
        public InvocationData InvocationData { get; set; }
        public DateTime? DeletedAt { get; set; }
        public bool InDeletedState { get; set; }
        public IDictionary<string, string> StateData { get; set; }
    }
}
