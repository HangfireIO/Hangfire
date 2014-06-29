using System;
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
        public DateTime? DeletedAt { get; set; }
        public bool InDeletedState { get; set; }
    }
}
