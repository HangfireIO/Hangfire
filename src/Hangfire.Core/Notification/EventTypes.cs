using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hangfire.Notification
{
    public class EventTypes
    {
        public enum Events
        {
            FailedJobPeak,
            ServerTimeout
        }
    }
}
