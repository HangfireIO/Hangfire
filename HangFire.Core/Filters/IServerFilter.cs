using System;

namespace HangFire
{
    public interface IServerFilter : IFilter
    {
        void InterceptPerform(HangFireJob job, Action action);
    }
}