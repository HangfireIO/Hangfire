using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hangfire.Notification
{
    public interface INotifier
    {
        void Notify(EventTypes eventType, string subject, string message);
        void Subscribe(EventTypes eventType, List<string> toEmails);
    }
}
