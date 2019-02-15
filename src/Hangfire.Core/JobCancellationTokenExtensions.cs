using Hangfire.Server;

namespace Hangfire
{
    internal static class JobCancellationTokenExtensions
    {
        public static bool IsAborted(this IJobCancellationToken jobCancellationToken)
        {
            if (jobCancellationToken is ServerJobCancellationToken serverJobCancellationToken)
            {
                // for ServerJobCancellationToken we may simply check IsAborted property
                // to prevent unnecessary creation of the linked CancellationTokenSource
                return serverJobCancellationToken.IsAborted;
            }
            
            return false;
        }
    }
}
