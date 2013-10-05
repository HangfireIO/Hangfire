using ServiceStack.Redis;

namespace HangFire.States
{
    public abstract class JobStateDescriptor
    {
        public virtual void Unapply(IRedisTransaction transaction, string jobId)
        {
        }
    }
}