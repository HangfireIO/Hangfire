using System;

namespace Hangfire.Server
{
    internal class ServerJobCancellationWatcher : IBackgroundProcess
    {
        public static readonly TimeSpan DefaultCheckInterval = TimeSpan.FromSeconds(5);
        
        private readonly TimeSpan _checkInterval;

        public ServerJobCancellationWatcher(TimeSpan checkInterval)
        {
            _checkInterval = checkInterval;
        }

        public void Execute(BackgroundProcessContext context)
        {
            using (var connection = context.Storage.GetConnection())
            {
                ServerJobCancellationToken.CheckAllCancellationTokens(connection);
            }

            context.Wait(_checkInterval);
        }

        public override string ToString()
        {
            return GetType().Name;
        }
    }
}
