namespace HangFire
{
    public interface IEnqueueInterceptor : IInterceptor
    {
        void InterceptEnqueue(Job job);
    }
}