using System;
using System.Collections.Generic;
using System.Linq;
using HangFire.Common.States;
using HangFire.Server;
using HangFire.Storage.Monitoring;

namespace HangFire.Storage
{
    public abstract class JobStorage
    {
        private static JobStorage _current;

        public static void SetCurrent(JobStorage storage)
        {
            _current = storage;
        }

        public static JobStorage Current { get { return _current; } }

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
