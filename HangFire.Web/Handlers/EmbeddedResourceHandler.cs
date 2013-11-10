// This file is part of HangFire.
// Copyright © 2013 Sergey Odinokov.
// 
// HangFire is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// HangFire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with HangFire.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Web;

namespace HangFire.Web
{
    internal abstract class EmbeddedResourceHandler : GenericHandler
    {
        public bool CacheResponse { get; set; }
        public string ContentType { get; set; }
        public Encoding ContentEncoding { get; set; }

        public override void ProcessRequest()
        {
            Response.ContentType = ContentType;

            if (CacheResponse)
            {
                Response.Cache.SetCacheability(HttpCacheability.Public);
                Response.Cache.SetExpires(DateTime.MaxValue);
            }

            if (ContentEncoding != null)
            {
                Response.ContentEncoding = ContentEncoding;
            }

            WriteResponse();
        }

        protected abstract void WriteResponse();

        protected void WriteResource(Assembly assembly, string resourceName)
        {
            if (assembly == null) throw new ArgumentNullException("assembly");
            if (resourceName == null) throw new ArgumentNullException("resourceName");

            using (var inputStream = assembly.GetManifestResourceStream(resourceName))
            {
                if (inputStream == null)
                {
                    throw new ArgumentException(string.Format(
                        @"Resource named {0} not found in assembly {1}.",
                        resourceName, assembly));
                }

                var buffer = new byte[Math.Min(inputStream.Length, 4096)];
                var readLength = inputStream.Read(buffer, 0, buffer.Length);
                while (readLength > 0)
                {
                    Response.OutputStream.Write(buffer, 0, readLength);
                    readLength = inputStream.Read(buffer, 0, buffer.Length);
                }
            }
        }
    }
}
