// This file is part of Hangfire. Copyright © 2016 Sergey Odinokov.
// 
// Permission to use, copy, modify, and/or distribute this software for any
// purpose with or without fee is hereby granted.
// 
// THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
// REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY
// AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
// INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM
// LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR
// OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR
// PERFORMANCE OF THIS SOFTWARE.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hangfire.Annotations;
using Microsoft.Owin;

namespace Hangfire.Dashboard
{
    internal sealed class OwinDashboardRequest : DashboardRequest
    {
        private const string FormCollectionKey = "Microsoft.Owin.Form#collection";
        private readonly IOwinContext _context;

        public OwinDashboardRequest([NotNull] IDictionary<string, object> environment)
        {
            if (environment == null) throw new ArgumentNullException(nameof(environment));
            _context = new OwinContext(environment);
        }

        public override string Method => _context.Request.Method;
        public override string Path => _context.Request.Path.Value;
        public override string PathBase => _context.Request.PathBase.Value;
        public override string LocalIpAddress => _context.Request.LocalIpAddress;
        public override string RemoteIpAddress => _context.Request.RemoteIpAddress;

        public override string GetQuery(string key) => _context.Request.Query[key];

        public override async Task<IList<string>> GetFormValuesAsync(string key)
        {
            IList<string> values;

            if(_context.Environment.ContainsKey(FormCollectionKey))
            {
                if(_context.Environment[FormCollectionKey] is IFormCollection)
                {
                    var form = (IFormCollection)_context.Request.Environment[FormCollectionKey];
                    values = form.GetValues(key);
                }
                else
                {
                    dynamic form = _context.Request.Environment[FormCollectionKey];
                    values = form.GetValues(key);
                }
            }
            else
            {
                var form = await _context.Request.ReadFormAsync().ConfigureAwait(false);
                values = form.GetValues(key);
            }
            
            return values ?? new List<string>();
        }
    }
}
