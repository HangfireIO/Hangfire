﻿// This file is part of Hangfire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// Hangfire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// Hangfire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with Hangfire. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using Hangfire.Logging;
using Hangfire.Server;
using Hangfire.States;
using Hangfire.Storage;

namespace Hangfire
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

        public abstract IMonitoringApi GetMonitoringApi();
        
        public abstract IStorageConnection GetConnection();

        public virtual IEnumerable<IServerComponent> GetComponents()
        {
            return Enumerable.Empty<IServerComponent>();
        }

        public virtual IEnumerable<IStateHandler> GetStateHandlers()
        {
            return Enumerable.Empty<IStateHandler>();
        }

        public virtual void WriteOptionsToLog(ILog logger)
        {
        }
    }
}
