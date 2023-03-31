// This file is part of Hangfire. Copyright © 2016 Hangfire OÜ.
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
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Hangfire.Annotations;

namespace Hangfire.Dashboard
{
    internal sealed class AspNetCoreDashboardResponse : DashboardResponse
    {
        private readonly HttpContext _context;

        public AspNetCoreDashboardResponse([NotNull] HttpContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            _context = context;
        }

        public override string ContentType
        {
            get { return _context.Response.ContentType; }
            set { _context.Response.ContentType = value; }
        }

        public override int StatusCode
        {
            get { return _context.Response.StatusCode; }
            set { _context.Response.StatusCode = value; }
        }

        public override Stream Body => _context.Response.Body;

        public override Task WriteAsync(string text)
        {
            return _context.Response.WriteAsync(text);
        }

        public override void SetExpire(DateTimeOffset? value)
        {
            _context.Response.Headers["Expires"] = value?.ToString("r", CultureInfo.InvariantCulture);
        }
    }
}
