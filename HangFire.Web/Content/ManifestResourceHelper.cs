using System;
using System.IO;

namespace HangFire.Web.Content
{
    public static class ManifestResourceHelper
    {
        public static void WriteResourceToStream(Stream outputStream, string resourceName)
        {
            if (outputStream == null) throw new ArgumentNullException("outputStream");
            if (resourceName == null) throw new ArgumentNullException("resourceName");
            if (resourceName.Length == 0) throw new ArgumentException(null, "resourceName");

            var thisType = typeof(ManifestResourceHelper);
            var thisAssembly = thisType.Assembly;

            using (var inputStream = thisAssembly.GetManifestResourceStream(thisType, resourceName))
            {
                if (inputStream == null)
                {
                    throw new Exception(string.Format(
                        @"Resource named {0}.{1} not found in assembly {2}.",
                        thisType.Namespace, resourceName, thisAssembly));
                }

                var buffer = new byte[Math.Min(inputStream.Length, 4096)];
                var readLength = inputStream.Read(buffer, 0, buffer.Length);
                while (readLength > 0)
                {
                    outputStream.Write(buffer, 0, readLength);
                    readLength = inputStream.Read(buffer, 0, buffer.Length);
                }
            }
        }
    }
}
