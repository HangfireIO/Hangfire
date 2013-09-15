using System;

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
    }
}
