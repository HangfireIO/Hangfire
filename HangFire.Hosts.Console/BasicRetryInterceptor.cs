using System;
using System.Threading;

namespace HangFire.Hosts
{
    public class BasicRetryFilter : IServerFilter
    {
        public void ServerFilter(ServerFilterContext filterContext)
        {
            var attemptsCount = 0;
            const int maxAttempts = 3;

            while (true)
            {
                try
                {
                    attemptsCount++;
                    filterContext.PerformAction();
                    return;
                }
                catch (Exception)
                {
                    if (attemptsCount > maxAttempts - 1)
                    {
                        throw;
                    }

                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }
            }
        }
    }
}
