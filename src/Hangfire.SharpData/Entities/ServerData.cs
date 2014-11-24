using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hangfire.SharpData.Entities {
    internal class ServerData {
        public int WorkerCount { get; set; }
        public string[] Queues { get; set; }
        public DateTime? StartedAt { get; set; }
    }
}
