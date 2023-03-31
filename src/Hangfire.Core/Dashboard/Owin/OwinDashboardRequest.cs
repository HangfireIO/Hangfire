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
