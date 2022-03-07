// This file is part of Hangfire. Copyright © 2015 Sergey Odinokov.
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

// ReSharper disable InconsistentNaming

using System;
using System.ComponentModel;
using System.Threading;
using Hangfire.Annotations;

namespace Hangfire
{
    public enum CompatibilityLevel
    {
        Version_110 = 110,
        Version_170 = 170,
    }

    public class GlobalConfiguration : IGlobalConfiguration
    {
        private static int _compatibilityLevel = (int)CompatibilityLevel.Version_110;

        public static IGlobalConfiguration Configuration { get; } = new GlobalConfiguration();

        internal static CompatibilityLevel CompatibilityLevel
        {
            get => (CompatibilityLevel)Volatile.Read(ref _compatibilityLevel);
            set => Volatile.Write(ref _compatibilityLevel, (int)value);
        }

        internal static bool HasCompatibilityLevel(CompatibilityLevel level)
        {
            return CompatibilityLevel >= level;
        }

        internal GlobalConfiguration()
        {
        }
    }

    public static class CompatibilityLevelExtensions
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

            GlobalConfiguration.CompatibilityLevel = compatibilityLevel;

            return configuration;
        }
    }
}
