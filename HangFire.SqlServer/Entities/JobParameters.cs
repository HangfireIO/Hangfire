using System;

namespace HangFire.SqlServer.Entities
{
    public class JobParameters
    {
        public Guid JobId { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
    }
}
