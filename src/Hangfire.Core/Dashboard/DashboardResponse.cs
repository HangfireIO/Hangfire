using System;
using System.IO;
using System.Threading.Tasks;

namespace Hangfire.Dashboard
{
    public abstract class DashboardResponse
    {
        public abstract string ContentType { get; set; }
        public abstract int StatusCode { get; set; }

        public abstract Stream Body { get; }

        public abstract void SetExpire(DateTimeOffset? value);
        public abstract Task WriteAsync(string text);
    }
}