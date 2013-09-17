using System.Linq;
using System.Text;
using System.Web;
using ServiceStack.Text;

namespace HangFire.Web
{
    internal class JsonStats : GenericHandler
    {
        public override void ProcessRequest()
        {
            var response = new
            {
                succeeded = JobStorage.SucceededCount(),
                failed = JobStorage.FailedCount(),
                workers = JobStorage.ProcessingJobs().Count(),
                scheduled = JobStorage.ScheduledCount(),
                enqueued = JobStorage.EnqueuedCount()
            };

            var serialized = JsonSerializer.SerializeToString(response);
            Response.ContentType = "application/json";
            Response.ContentEncoding = Encoding.UTF8;
            Response.Write(serialized);
        }
    }
}
