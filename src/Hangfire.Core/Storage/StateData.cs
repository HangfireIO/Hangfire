// This file is part of Hangfire. Copyright © 2013-2014 Hangfire OÜ.
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

using System.Collections.Generic;
using Hangfire.Annotations;

// ReSharper disable NotNullMemberIsNotInitialized - Let's trust them!

namespace Hangfire.Storage
{
    public class StateData
    {
        [NotNull]
        public string Name { get; set; }

        [CanBeNull]
        public string Reason { get; set; }

        [NotNull]
        public IDictionary<string, string> Data { get; set; } 
    }
}