// This file is part of Hangfire.
// Copyright © 2015 Sergey Odinokov.
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

// ReSharper disable InconsistentNaming
namespace Hangfire
{
    public enum CompatibilityLevel
    {
        Version_Pre_170 = 100,
        Version_170 = 170,
    }

    public class GlobalConfiguration : IGlobalConfiguration
    {
        public static IGlobalConfiguration Configuration { get; } = new GlobalConfiguration();

        public static CompatibilityLevel CompatibilityLevel = CompatibilityLevel.Version_Pre_170;

        public static bool HasCompatibilityLevel(CompatibilityLevel level)
        {
            return CompatibilityLevel >= level;
        }

        internal GlobalConfiguration()
        {
        }
    }
}
