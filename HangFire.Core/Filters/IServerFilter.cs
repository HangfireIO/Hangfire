using System;

namespace HangFire
{
    public interface IServerFilter : IFilter
    {
        void InterceptPerform(Worker worker, Action action);
    }
}