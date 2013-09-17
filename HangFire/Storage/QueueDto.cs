using System.Collections.Generic;

namespace HangFire.Storage
{
    public class QueueDto
    {
        public string Name { get; set; }
        public long Length { get; set; }
        public HashSet<string> Servers { get; set; }
    }
}