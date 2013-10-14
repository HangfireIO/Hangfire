using System;

namespace HangFire.Web
{
    internal static class Command
    {
        public static readonly Func<string, bool> Retry = x => JobStorage.RetryJob(x);
        public static readonly Func<string, bool> EnqueueScheduled = x => JobStorage.EnqueueScheduled(x);
    }
}
