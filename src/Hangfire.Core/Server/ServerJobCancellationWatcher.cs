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
