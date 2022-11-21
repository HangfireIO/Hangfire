// This file is part of Hangfire. Copyright © 2022 Hangfire OÜ.
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
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Hangfire.Common
{
    public sealed class TypeHelperSerializationBinder : SerializationBinder
#if NETSTANDARD2_0
            , Newtonsoft.Json.Serialization.ISerializationBinder
#endif
    {
        public override Type BindToType(string assemblyName, string typeName)
        {
            return TypeHelper.CurrentTypeResolver($"{typeName}, {assemblyName}");
        }

        public override void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            assemblyName = null;
            typeName = TypeHelper.CurrentTypeSerializer(serializedType);

            if (typeName == null) return;

            var delimiterIndex = GetAssemblyNameDelimiterIndex(typeName);

            if (delimiterIndex >= 0)
            {
                assemblyName = typeName.Substring(delimiterIndex + 1).Trim();
                typeName = typeName.Substring(0, delimiterIndex).Trim();
            }
        }

        private static int GetAssemblyNameDelimiterIndex(string typeName)
        {
            var level = 0;

            for (var index = 0; index < typeName.Length; index++)
            {
                var current = typeName[index];
                if (current == '[') level++;
                else if (current == ']') level--;
                else if (current == ',' && level == 0) return index;
            }

            return -1;
        }
    }
}