// This file is part of HangFire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// HangFire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// HangFire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with HangFire. If not, see <http://www.gnu.org/licenses/>.

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

        public abstract IMonitoringApi GetMonitoringApi();
        
        public abstract IStorageConnection GetConnection();

        public virtual IEnumerable<IThreadWrappable> GetComponents()
        {
            return Enumerable.Empty<IThreadWrappable>();
        }

        public virtual IEnumerable<StateHandler> GetStateHandlers()
        {
            return Enumerable.Empty<StateHandler>();
        }
    }
}
