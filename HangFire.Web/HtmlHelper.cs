using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HangFire.Web
{
    internal static class HtmlHelper
    {
        public static IHtmlString JobId(string jobId)
        {
            return new HtmlString(jobId.Substring(0, 8));
        }

        public static string JobType(string typeName)
        {
            var type = Type.GetType(typeName, throwOnError: false);

            if (type == null)
            {
                return typeName;
            }

            return type.FullName;
        }

        public static string FormatProperties(IDictionary<string, string> properties)
        {
            return @String.Join(", ", properties.Select(x => String.Format("{0}: \"{1}\"", x.Key, x.Value)));
        }

        public static IHtmlString QueueLabel(string queueName)
        {
            string label;
            if (queueName != null)
            {
                label = "<span class=\"label label-primary\">" + queueName + "</span>";
            }
            else
            {
                label = "<span class=\"label label-danger\"><i>Unknown</i></span>";
            }

            return new HtmlString(label);
        }
    }
}
