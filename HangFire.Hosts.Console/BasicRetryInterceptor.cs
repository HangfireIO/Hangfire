using System;
using System.Threading;

namespace HangFire.Interceptors
{
    public class BasicRetryInterceptor : IPerformInterceptor
    {
        public void InterceptPerform(Worker worker, Action action)
        {
            var attemptsCount = 0;
            const int maxAttempts = 3;

            while (true)
            {
                try
                {
                    attemptsCount++;
                    action();
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
