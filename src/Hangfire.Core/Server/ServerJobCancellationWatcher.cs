// This file is part of Hangfire. Copyright © 2019 Hangfire OÜ.
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
using Hangfire.Logging;

namespace Hangfire.Server
{
    internal class ServerJobCancellationWatcher : IBackgroundProcess
    {
        public static readonly TimeSpan DefaultCheckInterval = TimeSpan.FromSeconds(5);
        
        private readonly ILog _logger = LogProvider.GetLogger(typeof(ServerJobCancellationWatcher));
        private readonly TimeSpan _checkInterval;

        public ServerJobCancellationWatcher(TimeSpan checkInterval)
        {
            _checkInterval = checkInterval;
        }

        public void Execute(BackgroundProcessContext context)
        {
            _logger.Trace("Checking for aborted jobs...");

            using (var connection = context.Storage.GetConnection())
            {
                var abortedJobIds = ServerJobCancellationToken.CheckAllCancellationTokens(
                    context.ServerId,
                    connection,
                    context.StoppedToken);

                var aborted = false;

                foreach (var abortedJobId in abortedJobIds)
                {
                    _logger.Debug($"Job {abortedJobId.Item1} was aborted on worker {abortedJobId.Item2}.");
                    aborted = true;
                }

                if (!aborted)
                {
                    _logger.Trace("No newly aborted jobs found.");
                }
            }

            context.Wait(_checkInterval);
        }

        public override string ToString()
        {
            return GetType().Name;
        }
    }
}
