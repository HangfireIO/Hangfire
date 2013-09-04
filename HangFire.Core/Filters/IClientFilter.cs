namespace HangFire
{
    public interface IClientFilter : IFilter
    {
        void InterceptEnqueue(Job job);
    }
}