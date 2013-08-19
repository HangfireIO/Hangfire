using System;
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
                    succeeded = Storage.SucceededCount(),
                    failed = Storage.FailedCount(),
                    dispatchers = Storage.Dispatchers().Count(),
                    scheduled = Storage.ScheduledCount(),
                    enqueued = Storage.EnqueuedCount()
                };

            var serialized = JsonSerializer.SerializeToString(response);
            context.Response.ContentType = "application/json";
            context.Response.ContentEncoding = Encoding.UTF8;
            context.Response.Write(serialized);
        }
    }
}
