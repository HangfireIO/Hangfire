// This file is part of Hangfire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// Hangfire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// Hangfire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with Hangfire. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Reflection;
using System.Threading.Tasks;
using Hangfire.Annotations;
using Microsoft.Owin;

namespace Hangfire.Dashboard
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

        public Task Dispatch(RequestDispatcherContext context)
        {
            var owinContext = new OwinContext(context.OwinEnvironment);

            owinContext.Response.ContentType = _contentType;
            owinContext.Response.Expires = DateTime.Now.AddYears(1);

            WriteResponse(owinContext.Response);

            return Task.FromResult(true);
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

                inputStream.CopyTo(response.Body);
            }
        }
    }
}
