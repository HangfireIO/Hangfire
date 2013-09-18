using System;
using System.Web;

namespace HangFire.Web
{
    internal static class HtmlHelper
    {
        public static string JobType(string typeName)
        {
            var type = Type.GetType(typeName, throwOnError: false);

            if (type == null)
            {
                return typeName;
            }

            return type.FullName;
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
