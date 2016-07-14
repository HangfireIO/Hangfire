using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hangfire.Notification
{
    public class NotificationStore
    {
        private static readonly object LockObject = new object();
        private static NotificationStore _current;

        private readonly List<INotifier> _notifiers;
        
        public static NotificationStore Current
        {
            get
            {
                lock (LockObject)
                {
                    if (_current == null)
                    {
                        throw new InvalidOperationException("JobStorage.Current property value has not been initialized. You must set it before using Hangfire Client or Server API.");
                    }

                    return _current;
                }
            }
            set
            {
                lock (LockObject)
                {
                    _current = value;
                }
            }
        }

        public NotificationStore(List<INotifier> notifiers)
        {
            _notifiers = notifiers;
        }

        public void NotifyAll(EventTypes.Events eventType, string subject, string message)
        {
            foreach (var notifer in _notifiers)
            {
                notifer.Notify(eventType, subject, message);
            }
        }
    }
}
