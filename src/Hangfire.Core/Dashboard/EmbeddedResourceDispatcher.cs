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
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using Hangfire.Annotations;
#if NETFULL
using Microsoft.Owin;
#else
using Microsoft.AspNetCore.Http;
#endif

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
            if (contentType == null) throw new ArgumentNullException(nameof(contentType));
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));
            
            _assembly = assembly;
            _resourceName = resourceName;
            _contentType = contentType;
        }

        public Task Dispatch(RequestDispatcherContext context)
        {
#if NETFULL
            var owinContext = new OwinContext(context.OwinEnvironment);
            var response = owinContext.Response;
#else
            var response = context.Http.Response;
#endif

            response.ContentType = _contentType;

#if NETFULL
            response.Expires = DateTime.Now.AddYears(1);
#else
            response.Headers["Expires"] = DateTime.Now.AddYears(1).ToString("r", CultureInfo.InvariantCulture);
#endif

            WriteResponse(response);

            return Task.FromResult(true);
        }

        protected virtual void WriteResponse(
#if NETFULL
            IOwinResponse
#else
            HttpResponse
#endif
            response)
        {
            WriteResource(response, _assembly, _resourceName);
        }

        protected void WriteResource(
#if NETFULL
            IOwinResponse
#else
            HttpResponse
#endif
            response, Assembly assembly, string resourceName)
        {
            using (var inputStream = assembly.GetManifestResourceStream(resourceName))
            {
                if (inputStream == null)
                {
                    throw new ArgumentException($@"Resource with name {resourceName} not found in assembly {assembly}.");
                }

                inputStream.CopyTo(response.Body);
            }
        }
    }
}
