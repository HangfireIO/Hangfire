using System;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HangFire.Annotations;
using Microsoft.Owin;

namespace HangFire.Dashboard
{
    internal class EmbeddedResourceDispatcher : IRequestDispatcher
    {
        private readonly Assembly _assembly;
        private readonly string _resourceName;
        private readonly string _contentType;

        public EmbeddedResourceDispatcher(
            [NotNull] string contentType,
            [NotNull] Assembly assembly, 
            string resourceName)
        {
            if (contentType == null) throw new ArgumentNullException("contentType");
            if (assembly == null) throw new ArgumentNullException("assembly");
            
            _assembly = assembly;
            _resourceName = resourceName;
            _contentType = contentType;
        }

        public Task Dispatch(IOwinContext context, Match match)
        {
            context.Response.ContentType = _contentType;
            context.Response.Expires = DateTime.MaxValue;

            WriteResponse(context.Response);
            
            // TODO: replace with .NET 4.5's Task.FromResult
            var taskSource = new TaskCompletionSource<bool>();
            taskSource.SetResult(true);
            return taskSource.Task;
        }

        protected virtual void WriteResponse(IOwinResponse response)
        {
            WriteResource(response, _assembly, _resourceName);
        }

        protected void WriteResource(IOwinResponse response, Assembly assembly, string resourceName)
        {
            using (var inputStream = assembly.GetManifestResourceStream(resourceName))
            {
                if (inputStream == null)
                {
                    throw new ArgumentException(string.Format(
                        @"Resource with name {0} not found in assembly {1}.",
                        resourceName, assembly));
                }

                var buffer = new byte[Math.Min(inputStream.Length, 4096)];
                var readLength = inputStream.Read(buffer, 0, buffer.Length);
                while (readLength > 0)
                {
                    response.Write(buffer, 0, readLength);
                    readLength = inputStream.Read(buffer, 0, buffer.Length);
                }
            }
        }
    }
}
