// This file is part of HangFire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// HangFire is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// HangFire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with HangFire.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Threading;
using ServiceStack.Logging;

namespace HangFire.Server.Components
{
    public class ServerWatchdog : IThreadWrappable
    {
        private static readonly TimeSpan ServerTimeout = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(30); // TODO: increase interval

        private static readonly ILog Logger = LogManager.GetLogger(typeof(ServerWatchdog));

        private readonly ManualResetEvent _stopped = new ManualResetEvent(false);

        private readonly JobStorage _storage;
        
        public ServerWatchdog(JobStorage storage)
        {
            _storage = storage;
        }

        public void RemoveTimedOutServers(TimeSpan timeout)
        {
            using (var connection = _storage.GetConnection())
            {
                var serversRemoved = connection.RemoveTimedOutServers(timeout);
                if (serversRemoved != 0)
                {
                    Logger.Info(String.Format("{0} servers were removed due to timeout", serversRemoved));
                }
            }
        }

        void IThreadWrappable.Work()
        {
            try
            {
                Logger.Info("Server watchdog has been started.");

                while (true)
                {
                    JobServer.RetryOnException(
                        () => RemoveTimedOutServers(ServerTimeout), 
                        _stopped);

                    if (_stopped.WaitOne(CheckInterval))
                    {
                        break;
                    }
                }

                Logger.Info("Server watchdog has been stopped.");
            }
            catch (Exception ex)
            {
                Logger.Fatal("Unexpected exception caught.", ex);
            }
        }

        void IThreadWrappable.Dispose(Thread thread)
        {
            _stopped.Set();
            thread.Join();
        }
    }
}
