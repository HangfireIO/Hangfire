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

using System;
using System.ComponentModel;
using Hangfire.Annotations;

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

        internal CompatibilityLevel CompatibilityLevel = CompatibilityLevel.Version_Pre_170;

        internal static bool HasCompatibilityLevel(CompatibilityLevel level)
        {
            return ((GlobalConfiguration)Configuration).CompatibilityLevel >= level;
        }

        internal GlobalConfiguration()
        {
        }
    }

    public static class CompatibilityLevelGlobalConfigurationExtensions
    {
        public static IGlobalConfiguration SetDataCompatibilityLevel(
            [NotNull] this IGlobalConfiguration configuration,
            CompatibilityLevel compatibilityLevel)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

#if !NETSTANDARD1_3
            if (!Enum.IsDefined(typeof(CompatibilityLevel), compatibilityLevel))
                throw new InvalidEnumArgumentException(nameof(compatibilityLevel), (int) compatibilityLevel,
                    typeof(CompatibilityLevel));
#endif

            ((GlobalConfiguration) configuration).CompatibilityLevel = compatibilityLevel;

            return configuration;
        }
    }
}
