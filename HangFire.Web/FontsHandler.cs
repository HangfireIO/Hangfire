// This file is part of HangFire.
// Copyright © 2013-2014 Sergey Odinokov.
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

namespace HangFire.Web
{
    internal class FontsHandler : SingleResourceHandler
    {
        public FontsHandler(string fontName) 
            : base(typeof(FontsHandler).Assembly, HangFirePageFactory.GetContentResourceName("fonts", fontName))
        {
            CacheResponse = true;

            if (fontName.EndsWith(".eot", StringComparison.OrdinalIgnoreCase))
            {
                ContentType = "application/vnd.ms-fontobject";
            } 
            else if (fontName.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
            {
                ContentType = "image/svg+xml";
            }
            else if (fontName.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase))
            {
                ContentType = "application/octet-stream";
            }
            else if (fontName.EndsWith(".woff", StringComparison.OrdinalIgnoreCase))
            {
                ContentType = "application/font-woff";
            }
        }
    }
}
