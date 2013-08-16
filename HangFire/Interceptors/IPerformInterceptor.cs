using System;

namespace HangFire
{
    public interface IPerformInterceptor : IInterceptor
    {
        void InterceptPerform(Worker worker, Action action);
    }
}