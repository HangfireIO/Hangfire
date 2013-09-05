namespace HangFire
{
    public interface IClientFilter : IFilter
    {
        void InterceptEnqueue(JobDescription jobDescription);
    }
}