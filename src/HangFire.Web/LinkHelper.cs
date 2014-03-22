// This file is part of HangFire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// HangFire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// HangFire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with HangFire. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Text;
using System.Web;

namespace HangFire.Web
{
    internal static class LinkHelper
    {
        public static string LinkTo(this HttpRequestBase request, string link)
        {
            var sb = new StringBuilder(request.Path);
            var pathInfo = request.PathInfo;
            var pathInfoIndex = pathInfo.Length - 1;
            var sbIndex = sb.Length - 1;
            while (pathInfoIndex >= 0 && sb[sbIndex].Equals(pathInfo[pathInfoIndex]))
            {
                sb.Remove(sbIndex, 1);
                sbIndex--;
                pathInfoIndex--;
            }
            var basePath = sb.ToString();
            if (!basePath.EndsWith("/", StringComparison.OrdinalIgnoreCase)) basePath += "/";

            return basePath + link.TrimStart('/');
        }
    }
}
