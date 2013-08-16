using System;

namespace HangFire.Interceptors
{
    public interface IPerformInterceptor : IInterceptor
    {
        void InterceptPerform(Worker worker, Action action);
    }
}