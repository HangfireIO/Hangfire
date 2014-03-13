using System;
using System.Collections.Generic;
using System.Linq;
using HangFire.Common.States;
using HangFire.Server;
using HangFire.Storage;
using HangFire.Storage.Monitoring;

namespace HangFire
{
    public abstract class JobStorage
    {
        private static readonly object LockObject = new object();
        private static JobStorage _current;

        public static JobStorage Current
        {
            get
            {
                lock (LockObject)
                {
                    if (_current == null)
                    {
                        throw new InvalidOperationException();
                    }

                    return _current;
                }
            }
            set
            {
                lock (LockObject)
                {
                    if (value == null)
                    {
                        throw new ArgumentNullException("value");
                    }

                    _current = value;
                }
            }
        }

        public abstract IMonitoringApi CreateMonitoring();
        
        public abstract IStorageConnection CreateConnection();
        public abstract IStorageConnection CreatePooledConnection();

        public virtual IEnumerable<IThreadWrappable> GetComponents()
        {
            return Enumerable.Empty<IThreadWrappable>();
        }

        public virtual IEnumerable<JobStateHandler> GetStateHandlers()
        {
            return Enumerable.Empty<JobStateHandler>();
        }
    }
}
