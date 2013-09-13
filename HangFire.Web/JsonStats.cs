using System.Linq;
using System.Text;
using System.Web;
using ServiceStack.Text;

namespace HangFire.Web
{
    class JsonStats
    {
        public static void StatsResponse(HttpContextBase context)
        {
            var response = new
                {
                    succeeded = HangFireApi.SucceededCount(),
                    failed = HangFireApi.FailedCount(),
                    workers = HangFireApi.Workers().Count(),
                    scheduled = HangFireApi.ScheduledCount(),
                    enqueued = HangFireApi.EnqueuedCount()
                };

            var serialized = JsonSerializer.SerializeToString(response);
            context.Response.ContentType = "application/json";
            context.Response.ContentEncoding = Encoding.UTF8;
            context.Response.Write(serialized);
        }
    }
}
