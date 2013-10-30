using ServiceStack.Redis;

namespace HangFire.States
{
    public abstract class JobStateDescriptor
    {
        public virtual void Unapply(JobDescriptor descriptor, IRedisTransaction transaction)
        {
        }
    }
}