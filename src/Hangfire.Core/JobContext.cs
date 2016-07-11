using Hangfire.Storage;

namespace Hangfire
{
    public class JobContext : IJobContext
    {
        private readonly string jobId;
        private readonly string serverId;

        public JobContext(string jobId, IStorageConnection connection)
        {
            this.jobId = jobId;            
        }       

        public string JobId
        {
            get { return jobId; }
        }

        public static IJobContext Null
        {
            get { return null; }
        }
    }
}