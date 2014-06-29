using System.Reflection;
using System.Threading;
using ServiceStack.Redis;
using Xunit;

namespace Hangfire.Redis.Tests
{
    public class CleanRedisAttribute : BeforeAfterTestAttribute
    {
        private static readonly object GlobalLock = new object();

        public override void Before(MethodInfo methodUnderTest)
        {
            Monitor.Enter(GlobalLock);

            using (var client = RedisUtils.CreateClient())
            {
                client.FlushDb();
            }
        }

        public override void After(MethodInfo methodUnderTest)
        {
            Monitor.Exit(GlobalLock);
        }
    }
}
