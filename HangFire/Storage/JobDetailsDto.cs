using System.Collections.Generic;

namespace HangFire.Storage
{
    public class JobDetailsDto
    {
        public string Type { get; set; }
        public IDictionary<string, string> Arguments { get; set; }
        public IDictionary<string, string> Properties { get; set; }
        public IList<Dictionary<string, string>> History { get; set; }
    }
}
