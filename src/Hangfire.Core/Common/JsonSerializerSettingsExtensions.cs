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

using Newtonsoft.Json;
using System;
using System.Reflection;

namespace Hangfire.Common
{
    internal static class JsonSerializerSettingsExtensions
    {
        private static readonly PropertyInfo TypeNameAssemblyFormat = typeof(JsonSerializerSettings).GetRuntimeProperty(nameof(TypeNameAssemblyFormat));
        private static readonly PropertyInfo TypeNameAssemblyFormatHandling = typeof(JsonSerializerSettings).GetRuntimeProperty(nameof(TypeNameAssemblyFormatHandling));

        public static JsonSerializerSettings WithSimpleTypeNameAssemblyFormat(this JsonSerializerSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));
            
            var property = TypeNameAssemblyFormatHandling ?? TypeNameAssemblyFormat;
            property.SetValue(settings, Enum.Parse(property.PropertyType, "Simple"));

            return settings;
        }
    }
}
