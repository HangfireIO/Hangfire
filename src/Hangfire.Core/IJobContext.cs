namespace Hangfire
{
    public interface IJobContext
    {   
        string JobId { get; }
    }
}
