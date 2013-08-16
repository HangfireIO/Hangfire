namespace HangFire.Interceptors
{
    public interface IEnqueueInterceptor : IInterceptor
    {
        void InterceptEnqueue(Job job);
    }
}